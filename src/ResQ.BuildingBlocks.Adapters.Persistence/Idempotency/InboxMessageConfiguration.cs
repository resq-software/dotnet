using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ResQ.BuildingBlocks.Adapters.Persistence;

/// <summary>
/// The EF Core mapping for <see cref="InboxMessage"/>: table <c>inbox_messages</c> with a composite key
/// over <see cref="InboxMessage.MessageId"/> and <see cref="InboxMessage.Handler"/>.
/// </summary>
public sealed class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable("inbox_messages");
        builder.HasKey(message => new { message.MessageId, message.Handler });

        builder.Property(message => message.MessageId).IsRequired();
        builder.Property(message => message.Handler).IsRequired();
        builder.Property(message => message.ProcessedOnUtc).IsRequired();
    }
}
