namespace ResQ.BuildingBlocks.Adapters.Messaging;

/// <summary>
/// Retry policy for message processing: how many attempts, the base delay, the exponential growth
/// factor between attempts, and whether to add jitter. Consumed by the resilience pipeline that wraps
/// each dispatch.
/// </summary>
public sealed class RetryOptions
{
    /// <summary>The total number of processing attempts, including the first (default <c>3</c>).</summary>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>The delay before the first retry, grown exponentially for later retries (default <c>1s</c>).</summary>
    public TimeSpan BaseDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>The factor by which the delay grows each attempt (default <c>2.0</c>).</summary>
    public double BackoffMultiplier { get; set; } = 2.0;

    /// <summary>Whether to randomize the delay to avoid thundering herds (default <see langword="true"/>).</summary>
    public bool UseJitter { get; set; } = true;
}
