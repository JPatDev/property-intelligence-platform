using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyIntelligence.Modules.Documents.Domain;

namespace PropertyIntelligence.Modules.Documents.Infrastructure.Persistence;

internal sealed class DocumentRecordConfiguration : IEntityTypeConfiguration<DocumentRecord>
{
    public void Configure(EntityTypeBuilder<DocumentRecord> builder)
    {
        builder.ToTable("document");
        builder.HasKey(document => document.Id);
        builder.Property(document => document.DocumentType).HasMaxLength(100).IsRequired();
        builder.Property(document => document.FileName).HasMaxLength(255).IsRequired();
        builder.Property(document => document.StoragePath).HasMaxLength(1000).IsRequired();
        builder.Property(document => document.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(document => document.Version).IsConcurrencyToken();
        builder.HasIndex(document => new
        {
            document.OrganizationId,
            document.ClaimId,
            document.DocumentType,
            document.Status,
        });
        builder.HasQueryFilter(document => !document.IsDeleted);
    }
}
