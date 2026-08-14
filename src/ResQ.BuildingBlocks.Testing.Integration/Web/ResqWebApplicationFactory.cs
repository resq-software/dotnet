using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace ResQ.BuildingBlocks.Testing.Integration;

/// <summary>
/// A <see cref="WebApplicationFactory{TEntryPoint}"/> that bootstraps the application under test and
/// exposes a single override point, <see cref="ConfigureTestServices(IServiceCollection)"/>, for
/// swapping real adapters (the <c>DbContext</c>, integration-event publishers, and so on) with
/// container-backed or recording test doubles.
/// </summary>
/// <typeparam name="TEntryPoint">The application's entry-point type (usually its <c>Program</c>).</typeparam>
public class ResqWebApplicationFactory<TEntryPoint> : WebApplicationFactory<TEntryPoint>
    where TEntryPoint : class
{
    /// <summary>
    /// Wires <see cref="ConfigureTestServices(IServiceCollection)"/> into the test host's service
    /// configuration.
    /// </summary>
    /// <param name="builder">The web host builder supplied by the test host.</param>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ConfigureTestServices(ConfigureTestServices);
    }

    /// <summary>
    /// Override to replace registrations for the test run — for example, point the <c>DbContext</c>
    /// at a Testcontainers database or swap publishers for recording doubles. The base implementation
    /// does nothing.
    /// </summary>
    /// <param name="services">The test host's service collection.</param>
    protected virtual void ConfigureTestServices(IServiceCollection services)
    {
    }
}
