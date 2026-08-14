using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Widgets.Domain;

namespace Widgets.Infrastructure;

/// <summary>Maps <see cref="Widget"/> to its table, converting <see cref="WidgetId"/> to/from <see cref="Guid"/>.</summary>
public sealed class WidgetConfiguration : IEntityTypeConfiguration<Widget>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Widget> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("widgets");
        builder.HasKey(widget => widget.Id);

        builder.Property(widget => widget.Id)
            .HasConversion(id => id.Value, value => new WidgetId(value))
            .ValueGeneratedNever();

        builder.Property(widget => widget.Name).HasMaxLength(200).IsRequired();
        builder.Property(widget => widget.Quantity).IsRequired();
        builder.Property(widget => widget.CreatedOnUtc).IsRequired();
        builder.Property(widget => widget.UpdatedOnUtc).IsRequired();

        builder.Ignore(widget => widget.DomainEvents);
    }
}
