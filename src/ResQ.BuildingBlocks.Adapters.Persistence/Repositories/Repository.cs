using Microsoft.EntityFrameworkCore;
using ResQ.BuildingBlocks.Application;
using ResQ.BuildingBlocks.Domain;

namespace ResQ.BuildingBlocks.Adapters.Persistence;

/// <summary>
/// A read/write EF Core repository over an aggregate root. Derive a concrete repository from it and the
/// aggregate's application-level interface, then register both via <c>AddResqRepositories</c>.
/// </summary>
/// <typeparam name="TAggregate">The aggregate root type.</typeparam>
/// <typeparam name="TId">The aggregate's identity type.</typeparam>
/// <param name="dbContext">The unit-of-work context this repository reads and writes through.</param>
public abstract class Repository<TAggregate, TId>(DbContext dbContext) : IRepository<TAggregate, TId>
    where TAggregate : AggregateRoot<TId>
    where TId : notnull
{
    /// <summary>The change-tracking set backing this repository.</summary>
    protected DbSet<TAggregate> Set { get; } = dbContext.Set<TAggregate>();

    /// <inheritdoc />
    public async Task<TAggregate?> GetByIdAsync(TId id, CancellationToken ct = default) =>
        await Set.FindAsync([id], ct).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task AddAsync(TAggregate aggregate, CancellationToken ct = default) =>
        await Set.AddAsync(aggregate, ct).ConfigureAwait(false);

    /// <inheritdoc />
    public void Update(TAggregate aggregate) => Set.Update(aggregate);

    /// <inheritdoc />
    public void Remove(TAggregate aggregate) => Set.Remove(aggregate);

    /// <inheritdoc />
    public async Task<IReadOnlyList<TAggregate>> ListAsync(
        ISpecification<TAggregate> spec, CancellationToken ct = default) =>
        await Query(spec).ToListAsync(ct).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<TAggregate?> FirstOrDefaultAsync(
        ISpecification<TAggregate> spec, CancellationToken ct = default) =>
        await Query(spec).FirstOrDefaultAsync(ct).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<int> CountAsync(ISpecification<TAggregate> spec, CancellationToken ct = default) =>
        await Query(spec).CountAsync(ct).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<bool> AnyAsync(ISpecification<TAggregate> spec, CancellationToken ct = default) =>
        await Query(spec).AnyAsync(ct).ConfigureAwait(false);

    private IQueryable<TAggregate> Query(ISpecification<TAggregate> spec) =>
        SpecificationEvaluator.GetQuery(Set, spec);
}
