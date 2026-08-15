using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ResQ.BuildingBlocks.Domain;

namespace ResQ.BuildingBlocks.Adapters.Web;

/// <summary>
/// Maps the domain <see cref="Error"/>/<see cref="ErrorType"/> model onto RFC 7807
/// <see cref="ProblemDetails"/>. The <c>type</c> URI is synthesized deterministically from
/// <see cref="Error.Code"/> (a derived doc-reference, never a domain field), so services extend the
/// taxonomy through <c>overrides</c> + <c>docsBaseUri</c> instead of inventing a competing enum.
/// </summary>
public static class ProblemDetailsMapper
{
    /// <summary>
    /// Resolves the HTTP status code for an <see cref="ErrorType"/>. Defaults: Validation→400,
    /// NotFound→404, Conflict→409, Unauthorized→401, Failure→500. An entry in <paramref name="overrides"/>
    /// wins over the default for that type.
    /// </summary>
    /// <param name="type">The error classification to map.</param>
    /// <param name="overrides">Optional per-<see cref="ErrorType"/> status overrides (e.g. 403 or 429).</param>
    /// <returns>The HTTP status code.</returns>
    public static int ToStatusCode(ErrorType type, IReadOnlyDictionary<ErrorType, int>? overrides = null)
    {
        if (overrides is not null && overrides.TryGetValue(type, out var overridden))
        {
            return overridden;
        }

        return type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError,
        };
    }

    /// <summary>
    /// Builds a <see cref="ProblemDetails"/> from an <see cref="Error"/>. Status comes from
    /// <see cref="ToStatusCode(ErrorType, IReadOnlyDictionary{ErrorType, int})"/>; the title is derived
    /// from the <see cref="ErrorType"/>; <c>Extensions["code"]</c> carries the stable machine code;
    /// <see cref="ProblemDetails.Type"/> is a URI synthesized from <see cref="Error.Code"/>
    /// (<c>{docsBaseUri}/{code}</c>, or a <c>urn:resq:error:{code}</c> fallback); and
    /// <c>Extensions["traceId"]</c> carries the current activity or request identifier.
    /// </summary>
    /// <param name="error">The domain error to translate.</param>
    /// <param name="context">The current HTTP context, used for the trace identifier fallback.</param>
    /// <param name="overrides">Optional per-<see cref="ErrorType"/> status overrides.</param>
    /// <param name="docsBaseUri">Optional documentation base used to synthesize the <c>type</c> URI.</param>
    /// <returns>The populated problem details.</returns>
    [SuppressMessage(
        "Design",
        "CA1054:URI-like parameters should not be strings",
        Justification = "ProblemDetails.Type is itself a string per RFC 7807; docsBaseUri mirrors that surface.")]
    public static ProblemDetails ToProblemDetails(
        Error error,
        HttpContext? context = null,
        IReadOnlyDictionary<ErrorType, int>? overrides = null,
        string? docsBaseUri = null)
    {
        ArgumentNullException.ThrowIfNull(error);

        var problem = new ProblemDetails
        {
            Status = ToStatusCode(error.Type, overrides),
            Title = TitleFor(error.Type),
            Detail = error.Message,
            Type = SynthesizeTypeUri(error.Code, docsBaseUri),
        };

        problem.Extensions["code"] = error.Code;
        problem.Extensions["traceId"] = Activity.Current?.Id ?? context?.TraceIdentifier;
        return problem;
    }

    private static string SynthesizeTypeUri(string code, string? docsBaseUri) =>
        string.IsNullOrEmpty(docsBaseUri)
            ? $"urn:resq:error:{code}"
            : $"{docsBaseUri.TrimEnd('/')}/{code}";

    private static string TitleFor(ErrorType type) => type switch
    {
        ErrorType.Validation => "Validation failed",
        ErrorType.NotFound => "Resource not found",
        ErrorType.Conflict => "Conflict",
        ErrorType.Unauthorized => "Unauthorized",
        ErrorType.Forbidden => "Forbidden",
        _ => "Server error",
    };
}
