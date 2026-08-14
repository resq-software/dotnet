namespace ResQ.BuildingBlocks.Adapters.Persistence;

/// <summary>Options controlling the <see cref="OutboxRelay"/> polling loop.</summary>
public sealed class OutboxOptions
{
    /// <summary>How often the relay polls for unprocessed messages.</summary>
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>The maximum number of messages relayed per poll.</summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>How many times a single message is retried before it is left for inspection.</summary>
    public int MaxAttempts { get; set; } = 10;
}
