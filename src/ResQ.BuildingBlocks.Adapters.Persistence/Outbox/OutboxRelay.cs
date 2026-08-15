using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ResQ.BuildingBlocks.Application;

namespace ResQ.BuildingBlocks.Adapters.Persistence;

/// <summary>
/// A background service that polls the outbox for unprocessed <see cref="OutboxMessage"/> rows,
/// deserializes each back to its <see cref="IntegrationEvent"/>, and publishes it — at-least-once
/// delivery. Successfully relayed rows are stamped processed; failures increment the attempt count and
/// record the error. The publisher, serializer, type registry, and clock are resolved from the per-poll
/// scope (not captured in this singleton's constructor), so a scoped publisher or serializer is honored.
/// </summary>
/// <param name="scopes">Factory for the per-poll service scope that owns the context and collaborators.</param>
/// <param name="options">The relay options.</param>
/// <param name="logger">The relay logger.</param>
public sealed class OutboxRelay(
    IServiceScopeFactory scopes,
    IOptions<OutboxOptions> options,
    ILogger<OutboxRelay> logger) : BackgroundService
{
    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.Value.PollingInterval);

        do
        {
            await RelayPendingAsync(stoppingToken).ConfigureAwait(false);
        }
        while (await WaitForNextTickAsync(timer, stoppingToken).ConfigureAwait(false));
    }

    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "A relay iteration must not crash the host; the failure is logged and retried on the next tick.")]
    private async Task RelayPendingAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopes.CreateScope();
            var services = scope.ServiceProvider;

            // Resolve per-poll collaborators from the scope so scoped publishers/serializers are honored
            // rather than captured as singletons on this hosted service.
            var dbContext = services.GetRequiredService<DbContext>();
            var publisher = services.GetRequiredService<IIntegrationEventPublisher>();
            var serializer = services.GetRequiredService<IMessageSerializer>();
            var registry = services.GetRequiredService<IIntegrationEventTypeRegistry>();
            var clock = services.GetRequiredService<IClock>();

            var pending = await dbContext.Set<OutboxMessage>()
                .Where(message => message.ProcessedOnUtc == null && message.Attempts < options.Value.MaxAttempts)
                .OrderBy(message => message.OccurredOnUtc)
                .Take(options.Value.BatchSize)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            foreach (var message in pending)
            {
                await RelayOneAsync(message, publisher, serializer, registry, clock, cancellationToken)
                    .ConfigureAwait(false);
            }

            if (pending.Count > 0)
            {
                await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(exception, "Outbox relay iteration failed; retrying on the next tick.");
        }
    }

    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "A single poison message must not stop the batch; the error is recorded on the row.")]
    private async Task RelayOneAsync(
        OutboxMessage message,
        IIntegrationEventPublisher publisher,
        IMessageSerializer serializer,
        IIntegrationEventTypeRegistry registry,
        IClock clock,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!registry.TryResolve(message.Type, out var clrType))
            {
                RecordFailedAttempt(message, $"No CLR type registered for event type '{message.Type}'.");
                return;
            }

            if (serializer.Deserialize(message.Content, clrType) is not IntegrationEvent @event)
            {
                RecordFailedAttempt(message, $"Payload for event type '{message.Type}' did not deserialize to an integration event.");
                return;
            }

            await publisher.PublishAsync(@event, cancellationToken).ConfigureAwait(false);
            message.ProcessedOnUtc = clock.UtcNow;
            message.Error = null;
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(exception, "Failed to relay outbox message {MessageId}.", message.Id);
            RecordFailedAttempt(message, exception.Message);
        }
    }

    /// <summary>
    /// Records a failed relay attempt on the row and, when the attempt exhausts
    /// <see cref="OutboxOptions.MaxAttempts"/>, emits a terminal warning. An exhausted row drops out of the
    /// poll filter (<c>Attempts &lt; MaxAttempts</c>) and is never published again, so this warning is the
    /// only signal operators get that a message is stranded and needs manual inspection.
    /// </summary>
    private void RecordFailedAttempt(OutboxMessage message, string error)
    {
        message.Attempts++;
        message.Error = error;

        if (message.Attempts >= options.Value.MaxAttempts)
        {
            logger.LogWarning(
                "Outbox message {MessageId} of type {MessageType} exhausted its {MaxAttempts} relay attempts and will no longer be retried; it requires manual inspection. Last error: {Error}",
                message.Id,
                message.Type,
                options.Value.MaxAttempts,
                error);
        }
    }

    private static async Task<bool> WaitForNextTickAsync(PeriodicTimer timer, CancellationToken cancellationToken)
    {
        try
        {
            return await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
