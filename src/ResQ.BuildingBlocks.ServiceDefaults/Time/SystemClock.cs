using ResQ.BuildingBlocks.Application;

namespace ResQ.BuildingBlocks.ServiceDefaults;

/// <summary>
/// The production <see cref="IClock"/> — reads the real UTC wall-clock through
/// <see cref="TimeProvider.System"/>. Registered as a singleton by
/// <see cref="ServiceDefaultsExtensions.AddServiceDefaults{TBuilder}"/>; tests substitute a fake clock instead.
/// </summary>
public sealed class SystemClock : IClock
{
    /// <inheritdoc />
    public DateTimeOffset UtcNow => TimeProvider.System.GetUtcNow();
}
