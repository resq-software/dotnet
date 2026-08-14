using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ResQ.BuildingBlocks.Adapters.Web;

/// <summary>
/// Fluent helpers that attach a <see cref="ValidationEndpointFilter{TRequest}"/> to a route handler
/// or route group.
/// </summary>
public static class ValidationFilterExtensions
{
    /// <summary>Adds FluentValidation for <typeparamref name="TRequest"/> to a single route handler.</summary>
    /// <typeparam name="TRequest">The request type to validate.</typeparam>
    /// <param name="builder">The route handler builder.</param>
    /// <returns>The same builder for chaining.</returns>
    public static RouteHandlerBuilder WithValidation<TRequest>(this RouteHandlerBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddEndpointFilter<ValidationEndpointFilter<TRequest>>();
    }

    /// <summary>Adds FluentValidation for <typeparamref name="TRequest"/> to every handler in a group.</summary>
    /// <typeparam name="TRequest">The request type to validate.</typeparam>
    /// <param name="builder">The route group builder.</param>
    /// <returns>The same builder for chaining.</returns>
    public static RouteGroupBuilder WithValidation<TRequest>(this RouteGroupBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddEndpointFilter<ValidationEndpointFilter<TRequest>>();
    }
}
