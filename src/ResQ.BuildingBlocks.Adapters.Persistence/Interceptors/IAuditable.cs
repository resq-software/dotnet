namespace ResQ.BuildingBlocks.Adapters.Persistence;

/// <summary>
/// Opt-in auditing contract for entities that want created/modified timestamps stamped by the
/// <see cref="AuditInterceptor"/> at save time. Timestamps are set through methods (not public setters)
/// so the entity keeps control of its own state.
/// </summary>
/// <remarks>
/// Strict-layering consumers keep their domain free of this adapter concern and instead set timestamps
/// inside the command handler from an injected clock; the sample does exactly that.
/// </remarks>
public interface IAuditable
{
    /// <summary>Records when the entity was created (UTC).</summary>
    /// <param name="utc">The creation instant.</param>
    void SetCreated(DateTimeOffset utc);

    /// <summary>Records when the entity was last modified (UTC).</summary>
    /// <param name="utc">The modification instant.</param>
    void SetModified(DateTimeOffset utc);
}
