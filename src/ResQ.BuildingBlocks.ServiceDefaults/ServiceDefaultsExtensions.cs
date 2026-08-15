using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using ResQ.BuildingBlocks.Application;

namespace ResQ.BuildingBlocks.ServiceDefaults;

/// <summary>
/// Aspire-style shared defaults: one call from a host's composition root wires OpenTelemetry,
/// health checks, service discovery, standard HTTP resilience, and a testable <see cref="IClock"/>.
/// </summary>
/// <remarks>
/// <see cref="AddServiceDefaults{TBuilder}"/> deliberately does <b>not</b> register the CQRS observability
/// pipeline behaviors. Those are an explicit, ordered step the host takes via
/// <see cref="ObservabilityExtensions.AddResqObservabilityBehaviors"/> <b>after</b> the logging and
/// validation behaviors, so the realized order stays <c>Logging → Validation → Tracing → Metrics → Transaction</c>.
/// </remarks>
public static class ServiceDefaultsExtensions
{
    private const string OtlpEndpointVariable = "OTEL_EXPORTER_OTLP_ENDPOINT";

    /// <summary>The readiness/health endpoint path, shared by <see cref="MapDefaultEndpoints"/> and the tracing filter.</summary>
    private const string HealthEndpointPath = "/health";

    /// <summary>The liveness endpoint path, shared by <see cref="MapDefaultEndpoints"/> and the tracing filter.</summary>
    private const string AliveEndpointPath = "/alive";

    private static readonly string[] LiveTags = ["live"];

    /// <summary>
    /// Wires the full set of service defaults onto <paramref name="builder"/>: OpenTelemetry
    /// (<see cref="ConfigureOpenTelemetry{TBuilder}"/>), a default liveness health check
    /// (<see cref="AddDefaultHealthChecks{TBuilder}"/>), service discovery, HTTP-client defaults
    /// (standard resilience handler + service discovery), and a singleton <see cref="SystemClock"/> for
    /// <see cref="IClock"/>. Does not register observability pipeline behaviors.
    /// </summary>
    /// <typeparam name="TBuilder">The host application builder type.</typeparam>
    /// <param name="builder">The host application builder.</param>
    /// <returns>The same <paramref name="builder"/> for chaining.</returns>
    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ConfigureOpenTelemetry();
        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();
        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            // Retry/timeout/circuit-breaker for every HttpClient, then resolve service names.
            http.AddStandardResilienceHandler();
            http.AddServiceDiscovery();
        });

        builder.Services.AddSingleton<IClock, SystemClock>();

        return builder;
    }

    /// <summary>
    /// Configures OpenTelemetry logging, metrics, and tracing on <paramref name="builder"/>. Metrics add
    /// ASP.NET Core, HTTP-client, and runtime instrumentation plus the <see cref="ResqDiagnostics.MeterName"/>
    /// meter; tracing adds ASP.NET Core (filtering out the health probe endpoints) and HTTP-client sources
    /// plus both the <see cref="ResqDiagnostics.ActivitySourceName"/> source and the host app's own
    /// <c>ApplicationName</c> source (so an idiomatic <c>new ActivitySource(ApplicationName)</c> emits spans).
    /// The OTLP exporter is enabled only when the <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> configuration value is set.
    /// </summary>
    /// <typeparam name="TBuilder">The host application builder type.</typeparam>
    /// <param name="builder">The host application builder.</param>
    /// <returns>The same <paramref name="builder"/> for chaining.</returns>
    public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeScopes = true;
            logging.IncludeFormattedMessage = true;
        });

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddMeter(ResqDiagnostics.MeterName))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation(instrumentation =>
                    instrumentation.Filter = context => !IsHealthProbeRequest(context.Request.Path))
                .AddHttpClientInstrumentation()
                .AddSource(ResqDiagnostics.ActivitySourceName)
                .AddSource(builder.Environment.ApplicationName));

        AddOpenTelemetryExporters(builder);

        return builder;
    }

    /// <summary>
    /// Adds a default <c>self</c> liveness health check tagged <c>live</c>, surfaced by
    /// <see cref="MapDefaultEndpoints"/> at <c>/alive</c>.
    /// </summary>
    /// <typeparam name="TBuilder">The host application builder type.</typeparam>
    /// <param name="builder">The host application builder.</param>
    /// <returns>The same <paramref name="builder"/> for chaining.</returns>
    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), LiveTags);

        return builder;
    }

    /// <summary>
    /// Maps the health endpoints: <c>/health</c> (all checks must pass) and <c>/alive</c> (only checks
    /// tagged <c>live</c>). Mapped only outside Production, since unauthenticated health endpoints leak
    /// readiness detail; guard or authorize them explicitly before exposing them in Production.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>The same <paramref name="app"/> for chaining.</returns>
    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (!app.Environment.IsProduction())
        {
            app.MapHealthChecks(HealthEndpointPath);
            app.MapHealthChecks(AliveEndpointPath, new HealthCheckOptions
            {
                Predicate = registration => registration.Tags.Contains("live"),
            });
        }

        return app;
    }

    /// <summary>
    /// True when the request targets a health probe endpoint (<see cref="HealthEndpointPath"/> or
    /// <see cref="AliveEndpointPath"/>). Orchestrators poll these on a tight interval; excluding them from
    /// tracing keeps zero-value probe spans out of the trace stream and off the OTLP bill.
    /// </summary>
    private static bool IsHealthProbeRequest(PathString path) =>
        path.Equals(HealthEndpointPath, StringComparison.OrdinalIgnoreCase)
        || path.Equals(AliveEndpointPath, StringComparison.OrdinalIgnoreCase);

    private static void AddOpenTelemetryExporters<TBuilder>(TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration[OtlpEndpointVariable]);
        if (useOtlpExporter)
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }
    }
}
