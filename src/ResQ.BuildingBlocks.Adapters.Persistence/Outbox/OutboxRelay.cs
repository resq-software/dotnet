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
/// delivery. Successfully relayed rows are stamped processed. The publisher, serializer, type registry,
/// and clock are resolved from the per-poll scope (not captured in this singleton's constructor), so a
/// scoped publisher or serializer is honored.
/// </summary>
/// <remarks>
/// <para>
/// <b>Failure handling.</b> Only <i>message-specific</i> faults — an unresolvable event type or a payload
/// that fails to deserialize — consume a row's attempt budget and eventually park the row after
/// <see cref="OutboxOptions.MaxAttempts"/>. A publish failure is instead treated as a (potentially
/// broker-wide) outage: the row keeps its attempts, the current batch is abandoned mid-flight, and the
/// whole poll backs off and retries the same backlog on the next tick. This stops a transient broker
/// outage from silently burning every row's attempts and permanently stranding the backlog.
/// </para>
/// <para>
/// <b>⚠ Single-instance operation only.</b> This relay does <b>not</b> claim rows before publishing, so
/// running more than one instance against the same outbox table would publish every message once per
/// instance (duplicate delivery). Downstream consumers already dedupe via the inbox/idempotency store, so
/// duplicates are tolerable but wasteful. Run exactly one relay instance per outbox table (e.g. a single
/// replica, a leader-elected singleton, or a scheduled job). A provider-agnostic optimistic row claim was
/// evaluated and deliberately deferred: a clean batch claim needs an <c>ExecuteUpdate</c> over an
/// ordered, limited query, which EF Core cannot translate uniformly across providers (SQLite has no
/// <c>UPDATE … LIMIT</c>) without raw SQL. See the README for operational guidance.
/// </para>
/// </remarks>
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
                var outcome = await RelayOneAsync(message, publisher, serializer, registry, clock, cancellationToken)
                    .ConfigureAwait(false);

                // A publish failure is read as a broker-wide outage: stop the batch immediately without
                // consuming this (or any later) row's attempts. The rows already relayed in this pass are
                // still saved below; the untouched remainder stays pending and the whole backlog is retried
                // on the next poll tick — the poll interval is the back-off.
                if (outcome == RelayOutcome.BrokerUnavailable)
                {
                    break;
                }
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
        Justification = "A message-specific fault must not stop the batch (recorded on the row); a publish outage must not crash the relay (signalled as a broker outage).")]
    private async Task<RelayOutcome> RelayOneAsync(
        OutboxMessage message,
        IIntegrationEventPublisher publisher,
        IMessageSerializer serializer,
        IIntegrationEventTypeRegistry registry,
        IClock clock,
        CancellationToken cancellationToken)
    {
        // Message-specific faults (unresolvable type, undeserializable payload) are the row's own problem:
        // record the attempt so a genuinely poison row is eventually parked after MaxAttempts.
        if (!registry.TryResolve(message.Type, out var clrType))
        {
            RecordFailedAttempt(message, $"No CLR type registered for event type '{message.Type}'.");
            return RelayOutcome.MessageFailed;
        }

        IntegrationEvent @event;
        try
        {
            if (serializer.Deserialize(message.Content, clrType) is not IntegrationEvent deserialized)
            {
                RecordFailedAttempt(message, $"Payload for event type '{message.Type}' did not deserialize to an integration event.");
                return RelayOutcome.MessageFailed;
            }

            @event = deserialized;
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(exception, "Failed to deserialize outbox message {MessageId}.", message.Id);
            RecordFailedAttempt(message, exception.Message);
            return RelayOutcome.MessageFailed;
        }

        // A publish failure is NOT the row's fault — it is (assumed) a broker-wide outage. Do not consume an
        // attempt; signal the caller to abandon the batch and retry the whole backlog next poll.
        try
        {
            await publisher.PublishAsync(@event, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(
                exception,
                "Publishing outbox message {MessageId} failed; treating it as a broker outage. Backing off and retrying the batch on the next poll without consuming attempts.",
                message.Id);
            return RelayOutcome.BrokerUnavailable;
        }

        message.ProcessedOnUtc = clock.UtcNow;
        message.Error = null;
        return RelayOutcome.Processed;
    }

    /// <summary>The disposition of a single relay attempt, telling the batch loop how to proceed.</summary>
    private enum RelayOutcome
    {
        /// <summary>The message was published and stamped processed.</summary>
        Processed,

        /// <summary>A message-specific fault was recorded on the row; the batch continues.</summary>
        MessageFailed,

        /// <summary>Publishing failed (assumed broker-wide outage); the batch must back off and retry.</summary>
        BrokerUnavailable,
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
