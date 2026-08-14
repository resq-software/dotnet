using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;

namespace ResQ.BuildingBlocks.Adapters.Web;

/// <summary>
/// Wires the built-in OpenAPI document (net9 <c>AddOpenApi</c>/<c>MapOpenApi</c>) with a transformer
/// that advertises the shared RFC 7807 problem response, plus an optional Scalar UI in Development.
/// </summary>
public static class OpenApiExtensions
{
    /// <summary>
    /// Adds an OpenAPI document with an operation transformer that documents a default
    /// <c>application/problem+json</c> error response.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="documentName">The OpenAPI document name.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddResqOpenApi(this IServiceCollection services, string documentName = "v1")
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOpenApi(documentName, options =>
        {
            options.AddOperationTransformer((operation, context, cancellationToken) =>
            {
                operation.Responses ??= new OpenApiResponses();
                operation.Responses.TryAdd(
                    "500",
                    new OpenApiResponse
                    {
                        Description = "An unexpected error occurred (RFC 7807 application/problem+json).",
                    });
                return Task.CompletedTask;
            });
        });

        return services;
    }

    /// <summary>
    /// Maps the OpenAPI JSON endpoint and, when <paramref name="enableUi"/> is set and the host is in
    /// Development, the Scalar API reference UI.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <param name="enableUi">Whether to map the Scalar UI in Development.</param>
    /// <returns>The same web application for chaining.</returns>
    public static WebApplication MapResqOpenApi(this WebApplication app, bool enableUi = true)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapOpenApi();

        if (enableUi && app.Environment.IsDevelopment())
        {
            app.MapScalarApiReference();
        }

        return app;
    }
}
