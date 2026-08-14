using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ResQ.BuildingBlocks.Application;

namespace ResQ.BuildingBlocks.Adapters.Persistence;

/// <summary>
/// An EF Core save-changes interceptor that stamps created/modified timestamps on tracked
/// <see cref="IAuditable"/> entries. Registered as a <b>singleton</b> — it depends only on the singleton
/// <see cref="IClock"/>, so it is safe to share across a pooled <see cref="DbContext"/>.
/// </summary>
/// <param name="clock">The clock supplying the current instant.</param>
public sealed class AuditInterceptor(IClock clock) : SaveChangesInterceptor
{
    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context is not null)
        {
            var now = clock.UtcNow;
            foreach (var entry in context.ChangeTracker.Entries<IAuditable>())
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.SetCreated(now);
                    entry.Entity.SetModified(now);
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.SetModified(now);
                }
            }
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
