using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ResQ.BuildingBlocks.Adapters.Web;

/// <summary>
/// The single, ordered composition root for the ResQ web adapter: <see cref="AddResqWeb"/> registers
/// the services and <see cref="UseResqWeb"/> wires the middleware in a fixed order.
/// </summary>
public static class WebServiceCollectionExtensions
{
    /// <summary>
    /// Registers problem details + the exception handler, CORS, API versioning, OpenAPI, the scoped
    /// correlation middleware, pagination/correlation options, and endpoint discovery.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration (used for the CORS allowlist).</param>
    /// <param name="configure">Optional hook to customize <see cref="ResqWebOptions"/>.</param>
    /// <param name="endpointAssemblies">
    /// Assemblies to scan for <see cref="IEndpoint"/> implementations. Pass the API's own assembly
    /// (e.g. <c>typeof(Program).Assembly</c>) — this is required under <c>WebApplicationFactory</c>/test
    /// hosts, where <see cref="Assembly.GetEntryAssembly"/> resolves to the test runner rather than the
    /// API. When none are supplied, the entry assembly is used (correct for a plain <c>dotnet run</c>).
    /// </param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddResqWeb(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<ResqWebOptions>? configure = null,
        params Assembly[] endpointAssemblies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = new ResqWebOptions();
        configure?.Invoke(options);
        services.AddSingleton(options);

        services.AddProblemDetails(pd => pd.CustomizeProblemDetails = ProblemDetailsConfigurator.Customize);
        services.AddExceptionHandler<ProblemDetailsExceptionHandler>();
        services.AddResqCors(configuration);
        services.AddResqApiVersioning();
        services.AddResqOpenApi();
        services.AddScoped<CorrelationMiddleware>();

        services.Configure<CorrelationOptions>(c => c.HeaderName = options.Correlation.HeaderName);
        services.Configure<PaginationOptions>(p =>
        {
            p.DefaultPageSize = options.Pagination.DefaultPageSize;
            p.MaxPageSize = options.Pagination.MaxPageSize;
        });

        // The fixed middleware order in UseResqWeb includes authentication/authorization, so ensure
        // their services exist even when the host does not configure a scheme itself.
        services.AddAuthentication();
        services.AddAuthorization();

        // Prefer the assemblies the caller names; Assembly.GetEntryAssembly() is unreliable under
        // WebApplicationFactory/test hosts (it returns the test runner). Fall back to the entry
        // assembly for a plain `dotnet run`.
        var scanTargets = endpointAssemblies.Length > 0
            ? endpointAssemblies
            : Assembly.GetEntryAssembly() is { } entry
                ? [entry]
                : Array.Empty<Assembly>();

        if (scanTargets.Length > 0)
        {
            services.AddResqEndpoints(scanTargets);
        }

        return services;
    }

    /// <summary>
    /// Wires the ResQ middleware in the canonical order: exception handling → CORS → correlation →
    /// authentication → authorization → endpoints → OpenAPI.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>The same web application for chaining.</returns>
    public static WebApplication UseResqWeb(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var webOptions = app.Services.GetRequiredService<ResqWebOptions>();
        var corsOptions = app.Services.GetRequiredService<ResqCorsOptions>();

        app.UseExceptionHandler();
        app.UseCors(corsOptions.PolicyName);
        app.UseMiddleware<CorrelationMiddleware>();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapResqEndpoints();
        app.MapResqOpenApi(webOptions.EnableOpenApiUi);

        return app;
    }
}
