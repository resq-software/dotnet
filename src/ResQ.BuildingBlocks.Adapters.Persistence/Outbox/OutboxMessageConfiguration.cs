using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ResQ.BuildingBlocks.Adapters.Persistence;

/// <summary>
/// The EF Core mapping for <see cref="OutboxMessage"/>: table <c>outbox_messages</c>, keyed on
/// <see cref="OutboxMessage.Id"/>, with an index over unprocessed rows ordered by occurrence to serve
/// the relay's poll query.
/// </summary>
public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");
        builder.HasKey(message => message.Id);

        builder.Property(message => message.Type).IsRequired();
        builder.Property(message => message.Content).IsRequired();
        builder.Property(message => message.OccurredOnUtc).IsRequired();

        // A plain composite index (kept provider- and naming-convention-agnostic) serving the relay's
        // "unprocessed, oldest first" poll. Provider-specific partial/filtered indexes belong in a
        // migration, not in this store-agnostic configuration.
        builder.HasIndex(message => new { message.ProcessedOnUtc, message.OccurredOnUtc })
            .HasDatabaseName("ix_outbox_messages_unprocessed");
    }
}
