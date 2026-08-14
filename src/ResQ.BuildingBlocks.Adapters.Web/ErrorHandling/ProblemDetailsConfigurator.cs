using System.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace ResQ.BuildingBlocks.Adapters.Web;

/// <summary>
/// Supplies the shared <c>CustomizeProblemDetails</c> callback used by <c>AddProblemDetails</c>: it
/// stamps the trace and request identifiers into <c>Extensions</c> and sets <c>Instance</c> to the
/// request path on every generated problem response.
/// </summary>
public static class ProblemDetailsConfigurator
{
    /// <summary>The customization applied to every <see cref="ProblemDetailsContext"/>.</summary>
    public static Action<ProblemDetailsContext> Customize { get; } = static context =>
    {
        var http = context.HttpContext;
        context.ProblemDetails.Instance ??= http.Request.Path.Value;
        context.ProblemDetails.Extensions["traceId"] = Activity.Current?.Id ?? http.TraceIdentifier;
        context.ProblemDetails.Extensions["requestId"] = http.TraceIdentifier;
    };
}
