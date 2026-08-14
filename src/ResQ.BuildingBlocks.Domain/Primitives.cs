namespace ResQ.BuildingBlocks.Domain;

/// <summary>A domain event: something meaningful that happened inside the domain.</summary>
public interface IDomainEvent
{
    /// <summary>When the event occurred (UTC).</summary>
    DateTimeOffset OccurredOnUtc { get; }
}

/// <summary>
/// Base class for entities — domain objects with a stable identity (<typeparamref name="TId"/>).
/// Equality is by identity, not by attribute values.
/// </summary>
/// <typeparam name="TId">The identity type (e.g. a strongly-typed id or <see cref="System.Guid"/>).</typeparam>
public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>Creates the entity with its identity.</summary>
    protected Entity(TId id) => Id = id;

    /// <summary>The entity's stable identity.</summary>
    public TId Id { get; }

    /// <summary>Domain events raised by this entity, awaiting dispatch.</summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>Raises a domain event (dispatched by the infrastructure after persistence).</summary>
    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    /// <summary>Clears pending domain events (called by the dispatcher once handled).</summary>
    public void ClearDomainEvents() => _domainEvents.Clear();

    /// <inheritdoc />
    public bool Equals(Entity<TId>? other) =>
        other is not null && other.GetType() == GetType() && EqualityComparer<TId>.Default.Equals(other.Id, Id);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Entity<TId> entity && Equals(entity);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(GetType(), Id);
}

/// <summary>
/// An aggregate root — the entry point to an aggregate and the consistency boundary.
/// Only aggregate roots are loaded/saved via repositories.
/// </summary>
/// <typeparam name="TId">The identity type.</typeparam>
public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : notnull
{
    /// <summary>Creates the aggregate root with its identity.</summary>
    protected AggregateRoot(TId id) : base(id) { }
}

/// <summary>
/// Base class for value objects — immutable, identity-free, equal when their components are equal.
/// </summary>
public abstract class ValueObject : IEquatable<ValueObject>
{
    /// <summary>The atomic components that define equality for this value object.</summary>
    protected abstract IEnumerable<object?> GetEqualityComponents();

    /// <inheritdoc />
    public bool Equals(ValueObject? other) =>
        other is not null && other.GetType() == GetType() &&
        GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is ValueObject value && Equals(value);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var component in GetEqualityComponents())
        {
            hash.Add(component);
        }

        return hash.ToHashCode();
    }
}
