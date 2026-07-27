using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyIntelligence.Modules.Claims.Domain;

namespace PropertyIntelligence.Modules.Claims.Infrastructure.Persistence;

internal sealed class ClaimConfiguration : IEntityTypeConfiguration<Claim>
{
    public void Configure(EntityTypeBuilder<Claim> builder)
    {
        builder.ToTable("claim");
        builder.HasKey(claim => claim.Id);
        builder.Property(claim => claim.ClaimNumber).HasMaxLength(100).IsRequired();
        builder.Property(claim => claim.PolicyNumber).HasMaxLength(100);
        builder.Property(claim => claim.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(claim => claim.Version).IsConcurrencyToken();
        builder.HasIndex(claim => new { claim.OrganizationId, claim.ClaimNumber }).IsUnique();
        builder.HasIndex(claim => new { claim.OrganizationId, claim.Status });
        builder.HasIndex(claim => new { claim.OrganizationId, claim.PropertyId });
        builder.HasQueryFilter(claim => !claim.IsDeleted);
    }
}
