using ResQ.BuildingBlocks.Application;

namespace ResQ.BuildingBlocks.Testing;

/// <summary>
/// An <see cref="IUnitOfWork"/> that does nothing and reports zero affected rows. Use when a
/// collaborator requires a unit of work but the test does not care whether it is committed.
/// </summary>
public sealed class NoopUnitOfWork : IUnitOfWork
{
    /// <summary>Completes immediately and reports that no rows were affected.</summary>
    /// <param name="cancellationToken">Ignored.</param>
    /// <returns>A completed task whose result is <c>0</c>.</returns>
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
}
