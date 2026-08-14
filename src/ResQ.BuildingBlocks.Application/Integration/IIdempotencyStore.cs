namespace ResQ.BuildingBlocks.Application;

/// <summary>
/// Records which messages a given handler has already processed so consumers can skip duplicates
/// (inbox pattern). Adapters back it with a durable store; a no-op implementation opts out.
/// </summary>
public interface IIdempotencyStore
{
    /// <summary>Returns whether <paramref name="handler"/> has already processed <paramref name="messageId"/>.</summary>
    /// <param name="messageId">The unique message identifier.</param>
    /// <param name="handler">The logical handler name.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task<bool> HasProcessedAsync(string messageId, string handler, CancellationToken ct);

    /// <summary>Marks <paramref name="messageId"/> as processed by <paramref name="handler"/>.</summary>
    /// <param name="messageId">The unique message identifier.</param>
    /// <param name="handler">The logical handler name.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task MarkProcessedAsync(string messageId, string handler, CancellationToken ct);
}
