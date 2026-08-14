using System.Diagnostics.CodeAnalysis;
using ResQ.BuildingBlocks.Domain;

namespace ResQ.BuildingBlocks.Adapters.Web;

/// <summary>Aggregate options for the ResQ web adapter, applied by <c>AddResqWeb</c>.</summary>
public sealed class ResqWebOptions
{
    /// <summary>Whether the OpenAPI UI (Scalar) is mapped in Development.</summary>
    public bool EnableOpenApiUi { get; set; } = true;

    /// <summary>Pagination bounds bound into <see cref="PaginationOptions"/>.</summary>
    public PaginationOptions Pagination { get; set; } = new();

    /// <summary>Correlation settings bound into <see cref="CorrelationOptions"/>.</summary>
    public CorrelationOptions Correlation { get; set; } = new();

    /// <summary>
    /// Optional per-<see cref="ErrorType"/> HTTP status overrides passed to
    /// <see cref="ProblemDetailsMapper"/> (e.g. mapping a code to 403 or 429).
    /// </summary>
    public IReadOnlyDictionary<ErrorType, int>? StatusOverrides { get; set; }

    /// <summary>
    /// Optional documentation base used to synthesize the ProblemDetails <c>type</c> URI from an
    /// error code.
    /// </summary>
    [SuppressMessage(
        "Design",
        "CA1056:URI-like properties should not be strings",
        Justification = "ProblemDetails.Type is a string per RFC 7807; this base mirrors that surface.")]
    public string? DocsBaseUri { get; set; }
}
