using System.Security.Cryptography;
using System.Text.Json;
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
/// shutdown. Subclass this to bind a concrete source; the subclass name is the idempotency handler key. To
/// run several consumers over distinct sources, register each source with
/// <see cref="MessagingBuilder.AddKeyedMessageSource{TSource}"/> and annotate the subclass's base-constructor
/// <paramref name="source"/> parameter with <c>[FromKeyedServices(sourceKey)]</c> to select it.
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

    // Floor for the supervision back-off so a source that faults instantly (e.g. RetryOptions.BaseDelay set
    // to zero) cannot spin a hot re-subscribe loop while the broker is down.
    private static readonly TimeSpan MinBackoff = TimeSpan.FromMilliseconds(500);

    /// <summary>Drains the source until stopped, then awaits any in-flight work.</summary>
    /// <param name="stoppingToken">Signals that the service should stop.</param>
    /// <returns>A task representing the consumer loop.</returns>
    /// <remarks>
    /// The drain runs inside a supervision loop: a fault surfaced by <see cref="IMessageSource.ReadAllAsync"/>
    /// (for example a dropped broker connection) is logged, backed off, and the stream is re-subscribed
    /// rather than allowed to tear the host down. Only cancellation of <paramref name="stoppingToken"/> — or
    /// the source completing its stream on its own — exits the loop; in-flight work is then awaited.
    /// </remarks>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var opts = options.Value;
        using var semaphore = new SemaphoreSlim(Math.Max(1, opts.MaxConcurrency));
        var pipeline = BuildPipeline(opts.Retry);
        var inFlight = new List<Task>();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await foreach (var message in source.ReadAllAsync(stoppingToken).ConfigureAwait(false))
                {
                    await semaphore.WaitAsync(stoppingToken).ConfigureAwait(false);
                    inFlight.Add(ProcessAsync(message, opts, pipeline, semaphore, stoppingToken));
                    inFlight.RemoveAll(static task => task.IsCompleted);
                }

                // The source completed its stream without being cancelled (e.g. a closed channel). Nothing
                // more will ever arrive, so there is nothing left to supervise — stop draining.
                break;
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                // A source-level fault (dropped broker connection, a stray cancellation not tied to our
                // stopping token, and so on) must never stop the host. Log it, back off, then re-enter the
                // drain and re-subscribe. The back-off honors the stopping token and the loop re-checks it.
                logger.LogError(ex, "Message source faulted while draining; backing off before re-subscribing.");
                await BackOffAsync(opts.Retry, stoppingToken).ConfigureAwait(false);
            }
            catch (Exception) when (stoppingToken.IsCancellationRequested)
            {
                // Graceful shutdown requested (typically an OperationCanceledException from the source): stop
                // draining new messages and fall through to await in-flight work.
                break;
            }
        }

        await Task.WhenAll(inFlight).ConfigureAwait(false);
    }

    /// <summary>
    /// Waits out a supervision back-off before the source is re-subscribed, honoring the stopping token. Uses
    /// the retry policy's base delay (floored so a persistently faulting source cannot spin a hot loop) with
    /// the same jitter as the per-message pipeline, avoiding a thundering re-subscribe across replicas.
    /// </summary>
    private static async Task BackOffAsync(RetryOptions retry, CancellationToken stoppingToken)
    {
        var baseDelayMs = Math.Max(retry.BaseDelay.TotalMilliseconds, MinBackoff.TotalMilliseconds);
        var delayMs = ApplyJitter(baseDelayMs, retry.UseJitter);
        if (delayMs <= 0)
        {
            return;
        }

        try
        {
            await Task.Delay(TimeSpan.FromMilliseconds(delayMs), stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Shutdown raced the back-off delay; return so the supervision loop observes the token and exits.
        }
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

                    // Report the attempts actually made, never the configured ceiling: the handler always
                    // runs at least once (even with RetryOptions.MaxAttempts == 0), so the DLQ record must
                    // never claim "0 attempt(s)".
                    var attemptsMade = Math.Max(1, opts.Retry.MaxAttempts);
                    await sink.SendAsync(message, ex, attemptsMade, ct).ConfigureAwait(false);
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
                // Only retry plausibly-transient faults. Deterministic failures are classified as permanent
                // by IsTransient and dead-lettered on the first pass instead of burning the whole backoff
                // budget re-running a guaranteed-to-fail attempt.
                ShouldHandle = new PredicateBuilder().Handle<Exception>(IsTransient),
                MaxRetryAttempts = Math.Max(0, retry.MaxAttempts - 1),
                DelayGenerator = args =>
                {
                    var factor = Math.Pow(retry.BackoffMultiplier, args.AttemptNumber);
                    var delayMs = ApplyJitter(retry.BaseDelay.TotalMilliseconds * factor, retry.UseJitter);
                    return new ValueTask<TimeSpan?>(TimeSpan.FromMilliseconds(delayMs));
                },
            })
            .Build();
    }

    // Classifies a handler failure for the retry pipeline. Deterministic, self-identical failures are
    // permanent — a retry re-runs the exact same computation over the exact same bytes and fails the same
    // way — so they return false and drop straight through to the dead-letter path without consuming the
    // backoff budget. Everything else is assumed transient (transport/broker/database hiccups, timeouts) and
    // retried. OperationCanceledException never reaches here: cooperative cancellation is honored by the
    // pipeline and filtered out of the dead-letter catch, so it is intentionally not classified.
    private static bool IsTransient(Exception exception) => exception switch
    {
        // Malformed payload — re-deserializing the same bytes will always throw again.
        JsonException => false,

        // Payload failed validation — the same message will always be rejected. Fully qualified so this
        // transport file does not pull the FluentValidation namespace in for a single classification arm.
        FluentValidation.ValidationException => false,

        // Bad/unsupported argument or capability: a contract/coding error, not a blip (also covers the
        // ArgumentNullException subclass).
        ArgumentException => false,
        NotSupportedException => false,

        // The dispatcher raises InvalidOperationException for an unregistered message type or a body that
        // deserializes to null — both are structural poison messages, never fixed by retrying.
        InvalidOperationException => false,

        // Assume anything else (I/O, broker, database, timeout) may succeed on a later attempt.
        _ => true,
    };

    /// <summary>
    /// Spreads <paramref name="delayMs"/> by up to ±<see cref="JitterFraction"/> using a cryptographically
    /// strong RNG (never <see cref="System.Random"/>), so concurrent consumers do not retry in lockstep.
    /// Returns the delay unchanged when jitter is disabled or the delay is non-positive.
    /// </summary>
    private static double ApplyJitter(double delayMs, bool useJitter)
    {
        if (!useJitter || delayMs <= 0)
        {
            return delayMs;
        }

        var window = (int)Math.Clamp(delayMs * JitterFraction * 2, 1d, int.MaxValue);
        var offset = RandomNumberGenerator.GetInt32(0, window) - (delayMs * JitterFraction);
        return Math.Max(0, delayMs + offset);
    }
}
