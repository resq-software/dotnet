using Microsoft.EntityFrameworkCore;
using ResQ.BuildingBlocks.Adapters.Persistence;

namespace ResQ.Service.Infrastructure;

/// <summary>The EF Core context, wiring the ResQ outbox/inbox tables plus this service's configurations.</summary>
public sealed class SampleDbContext(DbContextOptions<SampleDbContext> options) : DbContext(options)
{
    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyResqInfrastructure();
        modelBuilder.ApplyConfigurationsFrom(typeof(SampleDbContext).Assembly);
    }
}
