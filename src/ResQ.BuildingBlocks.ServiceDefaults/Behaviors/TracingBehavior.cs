using System.Diagnostics;
using ResQ.BuildingBlocks.Application;
using ResQ.BuildingBlocks.Domain;

namespace ResQ.BuildingBlocks.ServiceDefaults;

/// <summary>
/// A pipeline behavior that starts an <see cref="Activity"/> around each request on the shared
/// <see cref="ResqDiagnostics.ActivitySource"/>, named for the request type. It sets an OK or error
/// status on the activity but deliberately tags <b>no</b> business data (no request/response field values),
/// so traces never leak payloads.
/// </summary>
/// <typeparam name="TRequest">The request (command or query) type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public sealed class TracingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        using var activity = ResqDiagnostics.ActivitySource.StartActivity(typeof(TRequest).Name);

        try
        {
            var response = await next().ConfigureAwait(false);
            activity?.SetStatus(
                response is Result { IsFailure: true } ? ActivityStatusCode.Error : ActivityStatusCode.Ok);
            return response;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }
}
