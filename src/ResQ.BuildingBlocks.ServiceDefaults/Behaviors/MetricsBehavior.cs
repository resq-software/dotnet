using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;
using ResQ.BuildingBlocks.Application;
using ResQ.BuildingBlocks.Domain;

namespace ResQ.BuildingBlocks.ServiceDefaults;

/// <summary>
/// A pipeline behavior that records request-handling metrics: a duration histogram and a failure counter,
/// each tagged by request name. Instruments are created through the injected
/// <see cref="IMeterFactory"/> using <see cref="ResqDiagnostics.MeterName"/> — the exact name
/// <see cref="ServiceDefaultsExtensions.ConfigureOpenTelemetry{TBuilder}"/> allow-lists via <c>AddMeter</c> —
/// so the emitting meter and the collected meter are guaranteed to match.
/// </summary>
/// <typeparam name="TRequest">The request (command or query) type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public sealed class MetricsBehavior<TRequest, TResponse>(IMeterFactory meterFactory)
    : IPipelineBehavior<TRequest, TResponse>
{
    private const string RequestTag = "request";

    // Instruments are cached per IMeterFactory (keyed weakly) rather than in a plain process-static field,
    // so their lifetime tracks the DI container that created the factory. A second in-process host (a
    // WebApplicationFactory test, a fresh container) gets a new factory and therefore its own instruments,
    // instead of recording into the first host's orphaned meter — which would silently drop every metric.
    // ConditionalWeakTable.GetValue builds the set once per factory; entries evict when the factory is GC'd.
    private static readonly ConditionalWeakTable<IMeterFactory, Instruments> InstrumentsByFactory = new();

    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        var instruments = GetInstruments(meterFactory);
        var requestName = typeof(TRequest).Name;
        var startTimestamp = Stopwatch.GetTimestamp();

        // Assume failure until the handler returns a non-failed Result. A thrown exception leaves this true,
        // so both a failed Result and an exception thrown out of the handler increment the failure counter.
        var failed = true;

        try
        {
            var response = await next().ConfigureAwait(false);
            failed = response is Result { IsFailure: true };
            return response;
        }
        finally
        {
            if (failed)
            {
                instruments.Failures.Add(1, new KeyValuePair<string, object?>(RequestTag, requestName));
            }

            var elapsedMs = Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;
            instruments.Duration.Record(elapsedMs, new KeyValuePair<string, object?>(RequestTag, requestName));
        }
    }

    private static Instruments GetInstruments(IMeterFactory factory) =>
        InstrumentsByFactory.GetValue(factory, CreateInstruments);

    private static Instruments CreateInstruments(IMeterFactory factory)
    {
        var meter = factory.Create(ResqDiagnostics.MeterName);
        var duration = meter.CreateHistogram<double>(
            "resq.cqrs.request.duration",
            unit: "ms",
            description: "Duration of CQRS request handling.");
        var failures = meter.CreateCounter<long>(
            "resq.cqrs.request.failures",
            unit: "{failure}",
            description: "Number of CQRS requests that failed — a failed Result or an exception from the handler.");
        return new Instruments(duration, failures);
    }

    private sealed record Instruments(Histogram<double> Duration, Counter<long> Failures);
}
