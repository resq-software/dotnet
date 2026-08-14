using Microsoft.EntityFrameworkCore;
using ResQ.BuildingBlocks.Application;
using ResQ.BuildingBlocks.Domain;

namespace ResQ.BuildingBlocks.Adapters.Persistence;

/// <summary>
/// The EF Core unit of work — the keystone of the persistence adapter. Its
/// <see cref="SaveChangesAsync"/> dispatches the domain events raised by tracked aggregates
/// <em>before</em> the underlying <see cref="DbContext.SaveChangesAsync(CancellationToken)"/>, so that
/// any rows a handler enqueues (for example an outbox row) commit atomically with the aggregate write.
/// </summary>
/// <remarks>
/// Registered <b>scoped</b> so the scoped <see cref="IDomainEventDispatcher"/> — and the scoped handlers
/// it resolves — run in the request scope. This replaces an EF save-changes interceptor: EF's internal
/// service provider does not contain application services, so an interceptor cannot resolve the
/// dispatcher. Commits must flow through this unit of work for domain events to dispatch.
/// </remarks>
public sealed class EfUnitOfWork(DbContext dbContext, IDomainEventDispatcher dispatcher) : IUnitOfWork
{
    /// <inheritdoc />
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await DispatchDomainEventsAsync(cancellationToken).ConfigureAwait(false);
        return await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task DispatchDomainEventsAsync(CancellationToken cancellationToken)
    {
        // Drain loop: a handler may raise further domain events (or enqueue rows), so keep dispatching
        // until no tracked aggregate has pending events, then let the underlying save persist everything.
        while (true)
        {
            var aggregates = dbContext.ChangeTracker
                .Entries<IHasDomainEvents>()
                .Where(entry => entry.Entity.DomainEvents.Count > 0)
                .Select(entry => entry.Entity)
                .ToList();

            if (aggregates.Count == 0)
            {
                return;
            }

            var domainEvents = aggregates
                .SelectMany(aggregate => aggregate.DomainEvents)
                .ToList();

            foreach (var aggregate in aggregates)
            {
                aggregate.ClearDomainEvents();
            }

            await dispatcher.DispatchAsync(domainEvents, cancellationToken).ConfigureAwait(false);
        }
    }
}
