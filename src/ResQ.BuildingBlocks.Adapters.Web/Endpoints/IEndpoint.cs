using Microsoft.AspNetCore.Routing;

namespace ResQ.BuildingBlocks.Adapters.Web;

/// <summary>
/// A self-registering endpoint module. Implementations map their routes onto the supplied
/// <see cref="IEndpointRouteBuilder"/> and are discovered by
/// <see cref="EndpointExtensions.AddResqEndpoints"/> / mapped by
/// <see cref="EndpointExtensions.MapResqEndpoints"/>.
/// </summary>
public interface IEndpoint
{
    /// <summary>Maps this module's routes onto the application's endpoint builder.</summary>
    /// <param name="app">The endpoint route builder to map onto.</param>
    void MapEndpoint(IEndpointRouteBuilder app);
}
