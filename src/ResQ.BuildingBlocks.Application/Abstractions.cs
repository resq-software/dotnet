using ResQ.BuildingBlocks.Domain;

namespace ResQ.BuildingBlocks.Application;

// ── CQRS request contracts (own your mediator abstraction — no third-party pipeline dependency) ──

/// <summary>A query — a read that returns <typeparamref name="TResponse"/> and never mutates state.</summary>
/// <typeparam name="TResponse">The read model returned.</typeparam>
public interface IQuery<TResponse>;

/// <summary>A command that mutates state and returns only success/failure.</summary>
public interface ICommand;

/// <summary>A command that mutates state and returns a <typeparamref name="TResponse"/> on success.</summary>
/// <typeparam name="TResponse">The value returned on success.</typeparam>
public interface ICommand<TResponse>;

/// <summary>Handles a <typeparamref name="TQuery"/>.</summary>
public interface IQueryHandler<in TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    /// <summary>Executes the query.</summary>
    Task<Result<TResponse>> Handle(TQuery query, CancellationToken cancellationToken);
}

/// <summary>Handles a <typeparamref name="TCommand"/> that returns no value.</summary>
public interface ICommandHandler<in TCommand>
    where TCommand : ICommand
{
    /// <summary>Executes the command.</summary>
    Task<Result> Handle(TCommand command, CancellationToken cancellationToken);
}

/// <summary>Handles a <typeparamref name="TCommand"/> that returns <typeparamref name="TResponse"/>.</summary>
public interface ICommandHandler<in TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    /// <summary>Executes the command.</summary>
    Task<Result<TResponse>> Handle(TCommand command, CancellationToken cancellationToken);
}

// ── Driven ports (the Core defines these; Infrastructure adapters implement them) ──

/// <summary>Commits the current unit of work atomically.</summary>
public interface IUnitOfWork
{
    /// <summary>Persists all tracked changes; returns the number of state entries written.</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

/// <summary>Abstracts the system clock so time is testable and deterministic.</summary>
public interface IClock
{
    /// <summary>The current instant (UTC).</summary>
    DateTimeOffset UtcNow { get; }
}

/// <summary>Dispatches domain events raised by aggregates after they are persisted.</summary>
public interface IDomainEventDispatcher
{
    /// <summary>Dispatches the given domain events to their handlers.</summary>
    Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
