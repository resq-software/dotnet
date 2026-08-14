using Asp.Versioning;
using Asp.Versioning.Builder;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ResQ.BuildingBlocks.Adapters.Web;

/// <summary>
/// Configures URL-segment API versioning (Asp.Versioning) with ResQ defaults and helpers for building
/// versioned minimal-API groups.
/// </summary>
public static class ApiVersioningExtensions
{
    /// <summary>
    /// Registers API versioning defaulting to v1.0, assuming the default version when unspecified,
    /// reporting supported versions, and reading the version from the URL segment.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional hook to further customize <see cref="ApiVersioningOptions"/>.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddResqApiVersioning(this IServiceCollection services, Action<ApiVersioningOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1.0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = new UrlSegmentApiVersionReader();
            configure?.Invoke(options);
        });

        return services;
    }

    /// <summary>Creates an <see cref="ApiVersionSet"/> covering the supplied version numbers.</summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <param name="versions">The version numbers (e.g. <c>1.0</c>, <c>2.0</c>).</param>
    /// <returns>The built version set.</returns>
    public static ApiVersionSet CreateResqVersionSet(this IEndpointRouteBuilder app, params double[] versions)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(versions);

        var builder = app.NewApiVersionSet();
        foreach (var version in versions)
        {
            builder.HasApiVersion(new ApiVersion(version));
        }

        return builder.ReportApiVersions().Build();
    }

    /// <summary>Maps a versioned route group under <paramref name="prefix"/> bound to <paramref name="set"/>.</summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <param name="prefix">The route prefix (e.g. <c>/api</c>).</param>
    /// <param name="set">The version set the group belongs to.</param>
    /// <returns>The versioned route group builder.</returns>
    public static RouteGroupBuilder MapVersionedGroup(this IEndpointRouteBuilder app, string prefix, ApiVersionSet set)
    {
        ArgumentNullException.ThrowIfNull(app);

        // The version segment must be in the route template for UrlSegmentApiVersionReader to bind it,
        // e.g. "/api" + v1.0 => "/api/v1/...". Without it no versioned route matches (404).
        return app.MapGroup($"{prefix}/v{{version:apiVersion}}").WithApiVersionSet(set);
    }
}
