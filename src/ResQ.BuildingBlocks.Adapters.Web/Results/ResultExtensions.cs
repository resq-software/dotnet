using Microsoft.AspNetCore.Http;
using ResQ.BuildingBlocks.Domain;

namespace ResQ.BuildingBlocks.Adapters.Web;

/// <summary>
/// Translates the domain <see cref="Result"/>/<see cref="Result{T}"/> model into minimal-API
/// <see cref="IResult"/> values via <see cref="TypedResults"/>, so the happy path never throws and
/// failures flow to RFC 7807 problem responses.
/// </summary>
public static class ResultExtensions
{
    /// <summary>Maps a non-generic <see cref="Result"/> to an HTTP result.</summary>
    /// <param name="result">The result to translate.</param>
    /// <param name="successStatus">The status code returned on success (204 No Content by default).</param>
    /// <returns>A status-code result on success, or a problem result on failure.</returns>
    public static IResult ToHttpResult(this Result result, int successStatus = StatusCodes.Status204NoContent)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.IsSuccess ? TypedResults.StatusCode(successStatus) : result.Error.Problem();
    }

    /// <summary>Maps a value-carrying <see cref="Result{T}"/> to an HTTP result.</summary>
    /// <typeparam name="T">The success value type.</typeparam>
    /// <param name="result">The result to translate.</param>
    /// <param name="onSuccess">Projects the value to an <see cref="IResult"/>; defaults to <c>200 OK</c>.</param>
    /// <returns>The projected success result, or a problem result on failure.</returns>
    public static IResult ToHttpResult<T>(this Result<T> result, Func<T, IResult>? onSuccess = null)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (result.IsFailure)
        {
            return result.Error.Problem();
        }

        onSuccess ??= static value => TypedResults.Ok(value);
        return onSuccess(result.Value);
    }

    /// <summary>Pattern-matches a <see cref="Result{T}"/> into success and failure branches.</summary>
    /// <typeparam name="T">The success value type.</typeparam>
    /// <param name="result">The result to match.</param>
    /// <param name="onSuccess">Projects the value on success.</param>
    /// <param name="onFailure">Projects the error on failure; defaults to a problem result.</param>
    /// <returns>The projected result.</returns>
    public static IResult Match<T>(this Result<T> result, Func<T, IResult> onSuccess, Func<Error, IResult>? onFailure = null)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(onSuccess);

        if (result.IsSuccess)
        {
            return onSuccess(result.Value);
        }

        return onFailure is not null ? onFailure(result.Error) : result.Error.Problem();
    }

    /// <summary>Maps an <see cref="Error"/> to an RFC 7807 problem result.</summary>
    /// <param name="error">The domain error to translate.</param>
    /// <param name="context">The current HTTP context, used for the trace identifier fallback.</param>
    /// <returns>A problem result carrying the mapped status and problem details.</returns>
    public static IResult Problem(this Error error, HttpContext? context = null)
    {
        ArgumentNullException.ThrowIfNull(error);
        return TypedResults.Problem(ProblemDetailsMapper.ToProblemDetails(error, context));
    }
}
