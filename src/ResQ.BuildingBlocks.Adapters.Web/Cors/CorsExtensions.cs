using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ResQ.BuildingBlocks.Adapters.Web;

/// <summary>Config-bound CORS options for the ResQ web adapter.</summary>
public sealed class ResqCorsOptions
{
    /// <summary>The name of the CORS policy registered and applied by the adapter.</summary>
    public string PolicyName { get; set; } = "resq";

    /// <summary>The explicit origin allowlist. A wildcard (<c>*</c>) is rejected with credentials.</summary>
    [SuppressMessage(
        "Performance",
        "CA1819:Properties should not return arrays",
        Justification = "Bound directly from configuration; an array matches the CORS WithOrigins surface.")]
    public string[] AllowedOrigins { get; set; } = [];

    /// <summary>Whether the policy allows credentials. Cannot be combined with a wildcard origin.</summary>
    public bool AllowCredentials { get; set; }
}

/// <summary>
/// Registers a CORS policy from a bound <see cref="ResqCorsOptions"/> allowlist. Unlike the ported Go
/// behavior, a wildcard origin combined with credentials is rejected rather than silently allowed.
/// </summary>
public static class CorsExtensions
{
    private const string SectionName = "Cors";

    /// <summary>
    /// Binds <see cref="ResqCorsOptions"/> from the <c>Cors</c> configuration section and registers the
    /// named policy with an explicit origin allowlist.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration to bind from.</param>
    /// <returns>The same service collection for chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the allowlist contains a wildcard (<c>*</c>) while credentials are enabled.
    /// </exception>
    public static IServiceCollection AddResqCors(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = configuration.GetSection(SectionName).Get<ResqCorsOptions>() ?? new ResqCorsOptions();
        var hasWildcard = Array.Exists(options.AllowedOrigins, static o => o == "*");

        if (hasWildcard && options.AllowCredentials)
        {
            throw new InvalidOperationException(
                "CORS misconfiguration: a wildcard origin ('*') cannot be combined with AllowCredentials.");
        }

        services.AddSingleton(options);
        services.AddCors(cors => cors.AddPolicy(options.PolicyName, policy =>
        {
            if (hasWildcard)
            {
                policy.AllowAnyOrigin();
            }
            else
            {
                policy.WithOrigins(options.AllowedOrigins);
            }

            policy.AllowAnyHeader().AllowAnyMethod();

            if (options.AllowCredentials)
            {
                policy.AllowCredentials();
            }
        }));

        return services;
    }
}
