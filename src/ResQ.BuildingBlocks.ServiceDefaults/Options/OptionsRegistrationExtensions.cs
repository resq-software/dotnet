using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ResQ.BuildingBlocks.ServiceDefaults;

/// <summary>
/// Helpers for registering strongly-typed options that fail fast — replacing scattered
/// <c>Environment.GetEnvironmentVariable</c> reads with a single validated binding.
/// </summary>
public static class OptionsRegistrationExtensions
{
    /// <summary>
    /// Binds <typeparamref name="TOptions"/> to the configuration section named <paramref name="sectionName"/>,
    /// validates it with its data-annotation attributes, and validates on start so a misconfigured service
    /// fails at boot rather than on first use.
    /// </summary>
    /// <typeparam name="TOptions">The options type to bind and validate.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="sectionName">The configuration section name to bind from.</param>
    /// <returns>The <see cref="OptionsBuilder{TOptions}"/> so callers can chain further validation.</returns>
    public static OptionsBuilder<TOptions> AddResqOptions<TOptions>(this IServiceCollection services, string sectionName)
        where TOptions : class
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrEmpty(sectionName);

        return services.AddOptions<TOptions>()
            .BindConfiguration(sectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }
}
