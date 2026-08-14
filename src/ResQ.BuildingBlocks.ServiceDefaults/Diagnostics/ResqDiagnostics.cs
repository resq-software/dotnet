using System.Diagnostics;

namespace ResQ.BuildingBlocks.ServiceDefaults;

/// <summary>
/// The single source of truth for the building blocks' telemetry names. The
/// <see cref="ActivitySource"/> name and the meter name are deliberately identical so a service can
/// allow-list both with one string, and so <c>AddSource</c>/<c>AddMeter</c> in
/// <see cref="ServiceDefaultsExtensions.ConfigureOpenTelemetry{TBuilder}"/> match the emitting instruments.
/// </summary>
/// <remarks>
/// There is intentionally <b>no</b> hand-newed static <see cref="System.Diagnostics.Metrics.Meter"/> here:
/// metrics instruments are created through <see cref="System.Diagnostics.Metrics.IMeterFactory"/> using
/// <see cref="MeterName"/>, so the DI-managed meter and the allow-listed name are always the same string.
/// </remarks>
public static class ResqDiagnostics
{
    /// <summary>The name registered with tracing via <c>AddSource</c> and used by the shared <see cref="ActivitySource"/>.</summary>
    public const string ActivitySourceName = "ResQ.BuildingBlocks";

    /// <summary>The name registered with metrics via <c>AddMeter</c> and passed to <c>IMeterFactory.Create</c>.</summary>
    public const string MeterName = "ResQ.BuildingBlocks";

    /// <summary>The shared <see cref="System.Diagnostics.ActivitySource"/> that pipeline behaviors start activities on.</summary>
    public static ActivitySource ActivitySource { get; } = new(ActivitySourceName);
}
