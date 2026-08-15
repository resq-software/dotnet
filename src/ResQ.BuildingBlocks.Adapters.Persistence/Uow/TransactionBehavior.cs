using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ResQ.BuildingBlocks.Application;

namespace ResQ.BuildingBlocks.Adapters.Persistence;

/// <summary>
/// A pipeline behavior that wraps <b>command</b> handling in a database transaction, using EF Core's
/// execution strategy so it composes with retry-on-failure. Queries pass straight through.
/// </summary>
/// <remarks>
/// Command-ness is detected via <see cref="CqrsRequest.IsCommand(System.Type)"/>, the application core's
/// shared classifier, which classifies through the implemented interfaces (catching every
/// <c>ICommand&lt;T&gt;</c>, which deliberately does not inherit <c>ICommand</c>). Register it last in the
/// pipeline so it is the innermost behavior around the handler.
/// </remarks>
/// <typeparam name="TRequest">The request (command or query) type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
/// <param name="dbContext">The context whose connection the transaction spans.</param>
/// <param name="logger">The behavior logger.</param>
public sealed class TransactionBehavior<TRequest, TResponse>(
    DbContext dbContext,
    ILogger<TransactionBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
{
    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!CqrsRequest.IsCommand(typeof(TRequest)))
        {
            return await next().ConfigureAwait(false);
        }

        var requestName = typeof(TRequest).Name;
        var strategy = dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            // Under EnableRetryOnFailure the strategy re-invokes this lambda after a transient failure.
            // A previous attempt may have left the handler's inserts/updates and raised domain events in
            // the ChangeTracker; replaying the handler over that dirty state would double-insert and
            // re-raise events. Clear it so every attempt starts from a clean slate. On the first attempt
            // the tracker is already empty (this behavior is the innermost, running just around the
            // handler), so the clear is a no-op there.
            dbContext.ChangeTracker.Clear();

            // 'await using' rolls the transaction back on dispose if CommitAsync was not reached,
            // so an exception thrown by the handler unwinds cleanly without an explicit catch.
            var transaction = await dbContext.Database
                .BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            await using (transaction.ConfigureAwait(false))
            {
                logger.LogDebug("Opened transaction for {Request}", requestName);
                var response = await next().ConfigureAwait(false);
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                logger.LogDebug("Committed transaction for {Request}", requestName);

                return response;
            }
        }).ConfigureAwait(false);
    }
}
