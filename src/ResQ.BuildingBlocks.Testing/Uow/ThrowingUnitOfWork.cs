using ResQ.BuildingBlocks.Application;

namespace ResQ.BuildingBlocks.Testing;

/// <summary>
/// An <see cref="IUnitOfWork"/> whose <see cref="SaveChangesAsync(CancellationToken)"/> always fails,
/// letting a test assert rollback / error-handling paths.
/// </summary>
public sealed class ThrowingUnitOfWork : IUnitOfWork
{
    private readonly Exception _exception;

    /// <summary>Initializes a new <see cref="ThrowingUnitOfWork"/>.</summary>
    /// <param name="exception">
    /// The exception to fail with. Defaults to an <see cref="InvalidOperationException"/> when <see langword="null"/>.
    /// </param>
    public ThrowingUnitOfWork(Exception? exception = null)
        => _exception = exception ?? new InvalidOperationException("The unit of work was configured to fail on SaveChangesAsync.");

    /// <summary>Returns a faulted task carrying the configured exception.</summary>
    /// <param name="cancellationToken">Ignored.</param>
    /// <returns>A faulted <see cref="Task{TResult}"/>.</returns>
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromException<int>(_exception);
}
