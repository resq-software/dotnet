using ResQ.BuildingBlocks.Application;

namespace ResQ.BuildingBlocks.Testing;

/// <summary>
/// A deterministic <see cref="IClock"/> whose current instant is fixed and only moves when the test
/// explicitly calls <see cref="Set(DateTimeOffset)"/> or <see cref="Advance(TimeSpan)"/>.
/// </summary>
/// <remarks>
/// Register this in place of the production clock (see
/// <see cref="TestDoublesExtensions.AddTestDoubles(Microsoft.Extensions.DependencyInjection.IServiceCollection)"/>)
/// to make time-dependent behavior reproducible.
/// </remarks>
public sealed class FakeClock : IClock
{
    /// <summary>
    /// Initializes a new <see cref="FakeClock"/>.
    /// </summary>
    /// <param name="start">
    /// The instant the clock reports until it is advanced. Defaults to
    /// <see cref="DateTimeOffset.UnixEpoch"/> so tests stay deterministic across runs.
    /// </param>
    public FakeClock(DateTimeOffset? start = null) => UtcNow = start ?? DateTimeOffset.UnixEpoch;

    /// <summary>Gets the current instant reported by the clock.</summary>
    public DateTimeOffset UtcNow { get; private set; }

    /// <summary>Sets the clock to an absolute instant.</summary>
    /// <param name="utc">The instant the clock should report from now on.</param>
    public void Set(DateTimeOffset utc) => UtcNow = utc;

    /// <summary>Advances the clock by a relative amount.</summary>
    /// <param name="by">The amount to move the clock forward (or backward, if negative).</param>
    public void Advance(TimeSpan by) => UtcNow += by;
}
