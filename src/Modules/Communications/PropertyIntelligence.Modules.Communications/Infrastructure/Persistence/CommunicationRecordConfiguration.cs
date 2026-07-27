using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyIntelligence.Modules.Communications.Domain;

namespace PropertyIntelligence.Modules.Communications.Infrastructure.Persistence;

internal sealed class CommunicationRecordConfiguration
    : IEntityTypeConfiguration<CommunicationRecord>
{
    public void Configure(EntityTypeBuilder<CommunicationRecord> builder)
    {
        builder.ToTable("communication");
        builder.HasKey(communication => communication.Id);
        builder.Property(communication => communication.CommunicationType)
            .HasMaxLength(100)
            .IsRequired();
        builder.Property(communication => communication.Direction)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();
        builder.Property(communication => communication.Channel)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();
        builder.Property(communication => communication.Subject)
            .HasMaxLength(500)
            .IsRequired();
        builder.Property(communication => communication.Recipient)
            .HasMaxLength(320)
            .IsRequired();
        builder.Property(communication => communication.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();
        builder.Property(communication => communication.Version).IsConcurrencyToken();
        builder.HasIndex(communication => new
        {
            communication.OrganizationId,
            communication.ClaimId,
            communication.CommunicationType,
            communication.Status,
        });
        builder.HasQueryFilter(communication => !communication.IsDeleted);
    }
}
