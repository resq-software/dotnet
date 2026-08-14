using ResQ.BuildingBlocks.Application;

namespace ResQ.BuildingBlocks.Testing;

/// <summary>
/// An <see cref="IUnitOfWork"/> that records how many times it was saved and returns a configurable
/// affected-row count, letting a test assert save behavior without a real database.
/// </summary>
public sealed class FakeUnitOfWork : IUnitOfWork
{
    /// <summary>Gets the number of times <see cref="SaveChangesAsync(CancellationToken)"/> has been called.</summary>
    public int SaveChangesCallCount { get; private set; }

    /// <summary>Gets or sets the affected-row count returned by <see cref="SaveChangesAsync(CancellationToken)"/>.</summary>
    public int AffectedRows { get; set; } = 1;

    /// <summary>Increments <see cref="SaveChangesCallCount"/> and returns <see cref="AffectedRows"/>.</summary>
    /// <param name="cancellationToken">Ignored.</param>
    /// <returns>A completed task whose result is <see cref="AffectedRows"/>.</returns>
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveChangesCallCount++;
        return Task.FromResult(AffectedRows);
    }
}
