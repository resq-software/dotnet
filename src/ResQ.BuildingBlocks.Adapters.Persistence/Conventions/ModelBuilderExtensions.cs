using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace ResQ.BuildingBlocks.Adapters.Persistence;

/// <summary>Model-building helpers that wire the persistence adapter's infrastructure tables and conventions.</summary>
public static class ModelBuilderExtensions
{
    /// <summary>
    /// Applies the outbox and inbox entity configurations. Call it from your context's
    /// <see cref="DbContext.OnModelCreating(ModelBuilder)"/> to enable the transactional outbox and inbox
    /// idempotency tables.
    /// </summary>
    /// <param name="modelBuilder">The model builder to configure.</param>
    /// <returns>The same model builder, for chaining.</returns>
    public static ModelBuilder ApplyResqInfrastructure(this ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
        modelBuilder.ApplyConfiguration(new InboxMessageConfiguration());
        return modelBuilder;
    }

    /// <summary>Applies every <see cref="IEntityTypeConfiguration{TEntity}"/> found in an assembly.</summary>
    /// <param name="modelBuilder">The model builder to configure.</param>
    /// <param name="assembly">The assembly to scan for entity configurations.</param>
    /// <returns>The same model builder, for chaining.</returns>
    public static ModelBuilder ApplyConfigurationsFrom(this ModelBuilder modelBuilder, Assembly assembly)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(assembly);
        return modelBuilder;
    }
}
