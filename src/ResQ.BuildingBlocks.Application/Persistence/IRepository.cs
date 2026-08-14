using ResQ.BuildingBlocks.Domain;

namespace ResQ.BuildingBlocks.Application;

/// <summary>
/// A read/write collection abstraction over an aggregate root. Adapters implement it against a store;
/// application handlers depend on it, not on the store.
/// </summary>
/// <typeparam name="TAggregate">The aggregate root type.</typeparam>
/// <typeparam name="TId">The aggregate's identity type.</typeparam>
public interface IRepository<TAggregate, TId>
    where TAggregate : AggregateRoot<TId>
    where TId : notnull
{
    /// <summary>Loads an aggregate by its identity, or <see langword="null"/> when absent.</summary>
    /// <param name="id">The aggregate identity.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task<TAggregate?> GetByIdAsync(TId id, CancellationToken ct = default);

    /// <summary>Stages a new aggregate for insertion.</summary>
    /// <param name="aggregate">The aggregate to add.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task AddAsync(TAggregate aggregate, CancellationToken ct = default);

    /// <summary>Stages an existing aggregate as modified.</summary>
    /// <param name="aggregate">The aggregate to update.</param>
    void Update(TAggregate aggregate);

    /// <summary>Stages an aggregate for deletion.</summary>
    /// <param name="aggregate">The aggregate to remove.</param>
    void Remove(TAggregate aggregate);

    /// <summary>Lists the aggregates matching a specification.</summary>
    /// <param name="spec">The query specification.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task<IReadOnlyList<TAggregate>> ListAsync(ISpecification<TAggregate> spec, CancellationToken ct = default);

    /// <summary>Returns the first aggregate matching a specification, or <see langword="null"/>.</summary>
    /// <param name="spec">The query specification.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task<TAggregate?> FirstOrDefaultAsync(ISpecification<TAggregate> spec, CancellationToken ct = default);

    /// <summary>Counts the aggregates matching a specification.</summary>
    /// <param name="spec">The query specification.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task<int> CountAsync(ISpecification<TAggregate> spec, CancellationToken ct = default);

    /// <summary>Returns whether any aggregate matches a specification.</summary>
    /// <param name="spec">The query specification.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task<bool> AnyAsync(ISpecification<TAggregate> spec, CancellationToken ct = default);
}

/// <summary>
/// The read-only subset of <see cref="IRepository{TAggregate,TId}"/>. Implementations query without
/// change tracking.
/// </summary>
/// <typeparam name="TAggregate">The aggregate root type.</typeparam>
/// <typeparam name="TId">The aggregate's identity type.</typeparam>
public interface IReadRepository<TAggregate, TId>
    where TAggregate : AggregateRoot<TId>
    where TId : notnull
{
    /// <summary>Loads an aggregate by its identity, or <see langword="null"/> when absent.</summary>
    /// <param name="id">The aggregate identity.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task<TAggregate?> GetByIdAsync(TId id, CancellationToken ct = default);

    /// <summary>Lists the aggregates matching a specification.</summary>
    /// <param name="spec">The query specification.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task<IReadOnlyList<TAggregate>> ListAsync(ISpecification<TAggregate> spec, CancellationToken ct = default);

    /// <summary>Returns the first aggregate matching a specification, or <see langword="null"/>.</summary>
    /// <param name="spec">The query specification.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task<TAggregate?> FirstOrDefaultAsync(ISpecification<TAggregate> spec, CancellationToken ct = default);

    /// <summary>Counts the aggregates matching a specification.</summary>
    /// <param name="spec">The query specification.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task<int> CountAsync(ISpecification<TAggregate> spec, CancellationToken ct = default);

    /// <summary>Returns whether any aggregate matches a specification.</summary>
    /// <param name="spec">The query specification.</param>
    /// <param name="ct">A token to cancel the operation.</param>
    Task<bool> AnyAsync(ISpecification<TAggregate> spec, CancellationToken ct = default);
}
