using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace ResQ.BuildingBlocks.Adapters.Web;

/// <summary>
/// An endpoint filter that runs every registered <see cref="IValidator{T}"/> against the first
/// <typeparamref name="TRequest"/> argument in the invocation and short-circuits with a
/// <c>ValidationProblem</c> (400) when any rule fails.
/// </summary>
/// <typeparam name="TRequest">The request type to validate.</typeparam>
/// <param name="validators">The validators registered for <typeparamref name="TRequest"/>.</param>
public sealed class ValidationEndpointFilter<TRequest>(IEnumerable<IValidator<TRequest>> validators) : IEndpointFilter
{
    /// <inheritdoc />
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var request = context.Arguments.OfType<TRequest>().FirstOrDefault();
        if (request is null)
        {
            return await next(context).ConfigureAwait(false);
        }

        var failures = new Dictionary<string, string[]>(StringComparer.Ordinal);
        foreach (var validator in validators)
        {
            var result = await validator.ValidateAsync(request, context.HttpContext.RequestAborted).ConfigureAwait(false);
            foreach (var group in result.Errors.GroupBy(e => e.PropertyName, StringComparer.Ordinal))
            {
                var messages = group.Select(e => e.ErrorMessage).ToArray();
                failures[group.Key] = failures.TryGetValue(group.Key, out var existing)
                    ? [.. existing, .. messages]
                    : messages;
            }
        }

        if (failures.Count > 0)
        {
            return TypedResults.ValidationProblem(failures);
        }

        return await next(context).ConfigureAwait(false);
    }
}
