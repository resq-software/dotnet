namespace ResQ.BuildingBlocks.Adapters.Messaging;

/// <summary>
/// Options that shape how <see cref="MessageConsumerService"/> drains a source: the degree of
/// concurrency, the retry policy, and whether idempotency short-circuiting is enabled.
/// </summary>
public sealed class ConsumerOptions
{
    /// <summary>The maximum number of messages processed concurrently (default <c>1</c>).</summary>
    public int MaxConcurrency { get; set; } = 1;

    /// <summary>The retry policy applied around each message's dispatch.</summary>
    public RetryOptions Retry { get; set; } = new();

    /// <summary>Whether to skip messages a handler has already processed via the idempotency store (default <see langword="true"/>).</summary>
    public bool EnableIdempotency { get; set; } = true;
}
