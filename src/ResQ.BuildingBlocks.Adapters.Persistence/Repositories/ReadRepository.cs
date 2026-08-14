using Microsoft.EntityFrameworkCore;
using ResQ.BuildingBlocks.Application;
using ResQ.BuildingBlocks.Domain;

namespace ResQ.BuildingBlocks.Adapters.Persistence;

/// <summary>
/// A read-only EF Core repository over an aggregate root. Every specification-driven query is forced
/// no-tracking, so results are never accidentally mutated and change-tracking overhead is avoided.
/// </summary>
/// <typeparam name="TAggregate">The aggregate root type.</typeparam>
/// <typeparam name="TId">The aggregate's identity type.</typeparam>
/// <param name="dbContext">The context this repository reads through.</param>
public class ReadRepository<TAggregate, TId>(DbContext dbContext) : IReadRepository<TAggregate, TId>
    where TAggregate : AggregateRoot<TId>
    where TId : notnull
{
    /// <summary>The set backing this repository.</summary>
    protected DbSet<TAggregate> Set { get; } = dbContext.Set<TAggregate>();

    /// <inheritdoc />
    public async Task<TAggregate?> GetByIdAsync(TId id, CancellationToken ct = default) =>
        await Set.FindAsync([id], ct).ConfigureAwait(false);

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
        SpecificationEvaluator.GetQuery(Set.AsNoTracking(), spec);
}
