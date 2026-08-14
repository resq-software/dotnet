namespace ResQ.BuildingBlocks.Adapters.Persistence;

/// <summary>Toggles for the optional pieces of the persistence adapter, set in <c>AddResqPersistence</c>.</summary>
public sealed class ResqPersistenceOptions
{
    /// <summary>
    /// When <see langword="true"/>, registers the transactional outbox (<see cref="EfOutbox"/>) and its
    /// relay (<see cref="OutboxRelay"/>). Off by default.
    /// </summary>
    public bool UseOutbox { get; set; }

    /// <summary>
    /// When <see langword="true"/> (the default), registers the open-generic
    /// <see cref="TransactionBehavior{TRequest,TResponse}"/> so commands run inside a transaction.
    /// </summary>
    public bool UseTransactionBehavior { get; set; } = true;

    /// <summary>
    /// When <see langword="true"/> (the default), attaches the <see cref="AuditInterceptor"/> so
    /// <see cref="IAuditable"/> entities get created/modified timestamps at save time.
    /// </summary>
    public bool EnableAudit { get; set; } = true;
}
