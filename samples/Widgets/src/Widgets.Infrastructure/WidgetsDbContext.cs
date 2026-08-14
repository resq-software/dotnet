using Microsoft.EntityFrameworkCore;
using ResQ.BuildingBlocks.Adapters.Persistence;

namespace Widgets.Infrastructure;

/// <summary>The EF Core context for the Widgets sample, wiring the ResQ outbox/inbox tables plus its own configurations.</summary>
public sealed class WidgetsDbContext(DbContextOptions<WidgetsDbContext> options) : DbContext(options)
{
    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyResqInfrastructure();
        modelBuilder.ApplyConfigurationsFrom(typeof(WidgetsDbContext).Assembly);
    }
}
