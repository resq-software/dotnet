using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace ResQ.BuildingBlocks.Adapters.Web;

/// <summary>Options for <see cref="CorrelationMiddleware"/>.</summary>
public sealed class CorrelationOptions
{
    /// <summary>The request/response header carrying the correlation identifier.</summary>
    public string HeaderName { get; set; } = "X-Correlation-ID";
}

/// <summary>
/// W3C-aware correlation middleware: it reads the inbound correlation header (falling back to the
/// current <see cref="Activity"/> id or the request trace identifier), records it as activity baggage,
/// and echoes it on the response. Implemented as <see cref="IMiddleware"/>, so it must be registered
/// with <c>AddScoped&lt;CorrelationMiddleware&gt;()</c> (done by <see cref="WebServiceCollectionExtensions"/>).
/// </summary>
/// <param name="options">The correlation options.</param>
public sealed class CorrelationMiddleware(IOptions<CorrelationOptions> options) : IMiddleware
{
    private readonly CorrelationOptions _options = options.Value;

    /// <inheritdoc />
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var headerName = _options.HeaderName;
        var correlationId = context.Request.Headers.TryGetValue(headerName, out var inbound) && !StringValues.IsNullOrEmpty(inbound)
            ? inbound.ToString()
            : Activity.Current?.Id ?? context.TraceIdentifier;

        Activity.Current?.SetBaggage("correlation.id", correlationId);

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[headerName] = correlationId;
            return Task.CompletedTask;
        });

        await next(context).ConfigureAwait(false);
    }
}
