using ResQ.BuildingBlocks.Domain;

namespace ResQ.BuildingBlocks.Application;

/// <summary>
/// Dispatches CQRS requests through the registered pipeline to their single handler and returns a
/// <see cref="Result"/>.
/// </summary>
/// <remarks>
/// A request must implement <b>exactly one</b> CQRS marker — <see cref="IQuery{TResponse}"/>,
/// <see cref="ICommand"/>, or <see cref="ICommand{TResponse}"/>. The matching overload resolves the
/// closed handler and the ordered <see cref="IPipelineBehavior{TRequest,TResponse}"/> chain from the
/// current scope.
/// </remarks>
public interface ISender
{
    /// <summary>Dispatches a query and returns its read model.</summary>
    /// <typeparam name="TResponse">The read model returned on success.</typeparam>
    /// <param name="query">The query to dispatch.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task<Result<TResponse>> Send<TResponse>(IQuery<TResponse> query, CancellationToken ct = default);

    /// <summary>Dispatches a command that returns only success or failure.</summary>
    /// <param name="command">The command to dispatch.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task<Result> Send(ICommand command, CancellationToken ct = default);

    /// <summary>Dispatches a command that returns a value on success.</summary>
    /// <typeparam name="TResponse">The value returned on success.</typeparam>
    /// <param name="command">The command to dispatch.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task<Result<TResponse>> Send<TResponse>(ICommand<TResponse> command, CancellationToken ct = default);
}
