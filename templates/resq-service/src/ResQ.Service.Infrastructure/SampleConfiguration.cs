using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResQ.Service.Domain;

namespace ResQ.Service.Infrastructure;

/// <summary>Maps <see cref="Sample"/> to its table, converting <see cref="SampleId"/> to/from <see cref="Guid"/>.</summary>
public sealed class SampleConfiguration : IEntityTypeConfiguration<Sample>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Sample> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("samples");
        builder.HasKey(sample => sample.Id);

        builder.Property(sample => sample.Id)
            .HasConversion(id => id.Value, value => new SampleId(value))
            .ValueGeneratedNever();

        builder.Property(sample => sample.Name).HasMaxLength(200).IsRequired();
        builder.Property(sample => sample.Quantity).IsRequired();
        builder.Property(sample => sample.CreatedOnUtc).IsRequired();
        builder.Property(sample => sample.UpdatedOnUtc).IsRequired();

        builder.Ignore(sample => sample.DomainEvents);
    }
}
