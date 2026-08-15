using System.Security.Cryptography;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using ResQ.BuildingBlocks.Application;

namespace ResQ.BuildingBlocks.Adapters.Messaging;

/// <summary>
/// A hosted background service that drains an <see cref="IMessageSource"/> and dispatches each message to
/// its integration-event handlers. Processing is bounded by <see cref="ConsumerOptions.MaxConcurrency"/>,
/// short-circuited by the idempotency store, wrapped in a Polly retry pipeline built from
/// <see cref="RetryOptions"/>, and dead-lettered when retries are exhausted. In-flight work is drained on
/// shutdown. Subclass this to bind a concrete source; the subclass name is the idempotency handler key.
/// </summary>
/// <param name="source">The message source to drain.</param>
/// <param name="scopes">The scope factory used to resolve scoped handlers per message.</param>
/// <param name="options">The consumer options.</param>
/// <param name="logger">The logger.</param>
public abstract class MessageConsumerService(
    IMessageSource source,
    IServiceScopeFactory scopes,
    IOptions<ConsumerOptions> options,
    ILogger<MessageConsumerService> logger) : BackgroundService
{
    private const double JitterFraction = 0.2;

    /// <summary>Drains the source until stopped, then awaits any in-flight work.</summary>
    /// <param name="stoppingToken">Signals that the service should stop.</param>
    /// <returns>A task representing the consumer loop.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var opts = options.Value;
        using var semaphore = new SemaphoreSlim(Math.Max(1, opts.MaxConcurrency));
        var pipeline = BuildPipeline(opts.Retry);
        var inFlight = new List<Task>();

        try
        {
            await foreach (var message in source.ReadAllAsync(stoppingToken).ConfigureAwait(false))
            {
                await semaphore.WaitAsync(stoppingToken).ConfigureAwait(false);
                inFlight.Add(ProcessAsync(message, opts, pipeline, semaphore, stoppingToken));
                inFlight.RemoveAll(static task => task.IsCompleted);
            }
        }
        catch (OperationCanceledException)
        {
            // Graceful shutdown requested — fall through and drain in-flight work.
        }

        await Task.WhenAll(inFlight).ConfigureAwait(false);
    }

    private async Task ProcessAsync(
        MessageEnvelope message,
        ConsumerOptions opts,
        ResiliencePipeline pipeline,
        SemaphoreSlim semaphore,
        CancellationToken ct)
    {
        try
        {
            var scope = scopes.CreateAsyncScope();
            await using (scope.ConfigureAwait(false))
            {
                var services = scope.ServiceProvider;
                var dispatcher = services.GetRequiredService<IntegrationEventDispatcher>();
                var handlerName = GetType().Name;

                var idempotency = opts.EnableIdempotency
                    ? services.GetRequiredService<IIdempotencyStore>()
                    : null;

                if (idempotency is not null &&
                    await idempotency.HasProcessedAsync(message.MessageId, handlerName, ct).ConfigureAwait(false))
                {
                    await source.AcknowledgeAsync(message, ct).ConfigureAwait(false);
                    return;
                }

                try
                {
                    await pipeline.ExecuteAsync(
                        async token => await dispatcher.DispatchAsync(message, token).ConfigureAwait(false),
                        ct).ConfigureAwait(false);

                    if (idempotency is not null)
                    {
                        await idempotency.MarkProcessedAsync(message.MessageId, handlerName, ct).ConfigureAwait(false);
                    }

                    await source.AcknowledgeAsync(message, ct).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // Retries are exhausted here — the Polly pipeline already ran every transient attempt
                    // internally. Take a single terminal action: dead-letter the message, then acknowledge so
                    // the transport drops it. Do NOT also nack-requeue; pairing dead-letter with a requeue would
                    // redeliver a poison message forever.
                    var sink = services.GetRequiredService<IDeadLetterSink>();
                    await sink.SendAsync(message, ex, opts.Retry.MaxAttempts, ct).ConfigureAwait(false);
                    await source.AcknowledgeAsync(message, ct).ConfigureAwait(false);
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Unexpected error while processing message {MessageId}.", message.MessageId);
        }
        finally
        {
            semaphore.Release();
        }
    }

    private static ResiliencePipeline BuildPipeline(RetryOptions retry)
    {
        return new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                MaxRetryAttempts = Math.Max(0, retry.MaxAttempts - 1),
                DelayGenerator = args =>
                {
                    var factor = Math.Pow(retry.BackoffMultiplier, args.AttemptNumber);
                    var delayMs = retry.BaseDelay.TotalMilliseconds * factor;

                    if (retry.UseJitter && delayMs > 0)
                    {
                        var window = (int)Math.Clamp(delayMs * JitterFraction * 2, 1d, int.MaxValue);
                        var offset = RandomNumberGenerator.GetInt32(0, window) - (delayMs * JitterFraction);
                        delayMs = Math.Max(0, delayMs + offset);
                    }

                    return new ValueTask<TimeSpan?>(TimeSpan.FromMilliseconds(delayMs));
                },
            })
            .Build();
    }
}
