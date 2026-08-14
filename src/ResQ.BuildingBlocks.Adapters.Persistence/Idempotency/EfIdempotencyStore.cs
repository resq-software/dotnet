using Microsoft.EntityFrameworkCore;
using ResQ.BuildingBlocks.Application;

namespace ResQ.BuildingBlocks.Adapters.Persistence;

/// <summary>
/// The EF Core idempotency store — backs the inbox pattern with an <see cref="InboxMessage"/> row per
/// <c>(messageId, handler)</c> pair, so a consumer can skip messages a handler has already processed.
/// </summary>
/// <param name="dbContext">The context the inbox rows are read from and written to.</param>
/// <param name="clock">The clock stamping the processed instant, matching EfOutbox and the audit interceptor.</param>
public sealed class EfIdempotencyStore(DbContext dbContext, IClock clock) : IIdempotencyStore
{
    /// <inheritdoc />
    public async Task<bool> HasProcessedAsync(string messageId, string handler, CancellationToken ct) =>
        await dbContext.Set<InboxMessage>()
            .AnyAsync(message => message.MessageId == messageId && message.Handler == handler, ct)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task MarkProcessedAsync(string messageId, string handler, CancellationToken ct)
    {
        dbContext.Set<InboxMessage>().Add(new InboxMessage
        {
            MessageId = messageId,
            Handler = handler,
            ProcessedOnUtc = clock.UtcNow,
        });

        // Persist immediately: the consumer flow has no separate unit-of-work commit for the inbox row.
        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
