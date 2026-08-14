using ResQ.BuildingBlocks.Application;

namespace ResQ.BuildingBlocks.Adapters.Messaging;

/// <summary>
/// An <see cref="IIdempotencyStore"/> that never records anything and always reports "not processed".
/// The default when no durable (e.g. EF inbox) store is registered — it opts idempotency out.
/// </summary>
public sealed class NullIdempotencyStore : IIdempotencyStore
{
    /// <summary>Always returns <see langword="false"/> — nothing is ever recorded as processed.</summary>
    /// <param name="messageId">The unique message identifier.</param>
    /// <param name="handler">The logical handler name.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A task producing <see langword="false"/>.</returns>
    public Task<bool> HasProcessedAsync(string messageId, string handler, CancellationToken ct) =>
        Task.FromResult(false);

    /// <summary>No-op — nothing is recorded.</summary>
    /// <param name="messageId">The unique message identifier.</param>
    /// <param name="handler">The logical handler name.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    /// <returns>A completed task.</returns>
    public Task MarkProcessedAsync(string messageId, string handler, CancellationToken ct) =>
        Task.CompletedTask;
}
