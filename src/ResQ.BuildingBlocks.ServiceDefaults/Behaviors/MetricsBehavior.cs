using System.Diagnostics;
using System.Diagnostics.Metrics;
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

    // Cached per closed generic: the factory returns a shared meter for the name, so the instruments are
    // created once rather than on every (transient) behavior instantiation. Access is via GetInstruments,
    // whose double-checked lock serializes the one-time build so concurrent first calls cannot each
    // construct a duplicate set of instruments.
    private static volatile Instruments? _instruments;
    private static readonly object InstrumentsLock = new();

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

    private static Instruments GetInstruments(IMeterFactory factory)
    {
        var instruments = _instruments;
        if (instruments is not null)
        {
            return instruments;
        }

        lock (InstrumentsLock)
        {
            return _instruments ??= CreateInstruments(factory);
        }
    }

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
