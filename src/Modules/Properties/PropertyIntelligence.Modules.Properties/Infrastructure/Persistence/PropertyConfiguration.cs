using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyIntelligence.Modules.Properties.Domain;

namespace PropertyIntelligence.Modules.Properties.Infrastructure.Persistence;

internal sealed class PropertyConfiguration : IEntityTypeConfiguration<Property>
{
    public void Configure(EntityTypeBuilder<Property> builder)
    {
        builder.ToTable("property");
        builder.HasKey(property => property.Id);

        builder.OwnsOne(property => property.Address, address =>
        {
            address.Property(value => value.Street)
                .HasColumnName("street")
                .HasMaxLength(200)
                .IsRequired();
            address.Property(value => value.City)
                .HasColumnName("city")
                .HasMaxLength(100)
                .IsRequired();
            address.Property(value => value.State)
                .HasColumnName("state")
                .HasMaxLength(2)
                .IsRequired();
            address.Property(value => value.PostalCode)
                .HasColumnName("postal_code")
                .HasMaxLength(10)
                .IsRequired();
        });
        builder.Navigation(property => property.Address).IsRequired();

        builder.Property(property => property.AddressKey).HasMaxLength(415).IsRequired();
        builder.Property(property => property.County).HasMaxLength(100).IsRequired();
        builder.Property(property => property.ParcelNumber).HasMaxLength(100);
        builder.Property(property => property.PropertyType).HasMaxLength(100);
        builder.Property(property => property.RoofType).HasMaxLength(100);
        builder.Property(property => property.OwnerName).HasMaxLength(200);
        builder.Property(property => property.OccupancyType).HasMaxLength(100);
        builder.Property(property => property.Location).HasColumnType("geometry (point, 4326)");
        builder.Property(property => property.Version).IsConcurrencyToken();
        builder.Ignore(property => property.Latitude);
        builder.Ignore(property => property.Longitude);

        builder.HasIndex(property => new { property.OrganizationId, property.AddressKey })
            .IsUnique();
        builder.HasIndex(property => new { property.OrganizationId, property.ParcelNumber })
            .IsUnique()
            .HasFilter("parcel_number IS NOT NULL");
        builder.HasIndex(property => new { property.OrganizationId, property.County });
        builder.HasIndex(property => property.Location).HasMethod("gist");
        builder.HasQueryFilter(property => !property.IsDeleted);
    }
}
