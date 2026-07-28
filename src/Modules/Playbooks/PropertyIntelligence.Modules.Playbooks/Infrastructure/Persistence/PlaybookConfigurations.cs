using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyIntelligence.Modules.Playbooks.Domain;

namespace PropertyIntelligence.Modules.Playbooks.Infrastructure.Persistence;

internal sealed class PlaybookConfiguration : IEntityTypeConfiguration<Playbook>
{
    public void Configure(EntityTypeBuilder<Playbook> builder)
    {
        builder.ToTable("playbook");
        builder.HasKey(playbook => playbook.Id);
        builder.Property(playbook => playbook.Id).ValueGeneratedNever();
        builder.Property(playbook => playbook.Key).HasMaxLength(100).IsRequired();
        builder.Property(playbook => playbook.Name).HasMaxLength(200).IsRequired();
        builder.Property(playbook => playbook.Description).HasMaxLength(2000).IsRequired();
        builder.Property(playbook => playbook.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(playbook => playbook.Version).IsConcurrencyToken();
        builder.HasIndex(playbook => new { playbook.OrganizationId, playbook.Key }).IsUnique();
        builder.HasIndex(playbook => new { playbook.OrganizationId, playbook.Status });
        builder.HasMany(playbook => playbook.Versions)
            .WithOne()
            .HasForeignKey("PlaybookId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(playbook => playbook.Versions)
            .HasField("_versions")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class PlaybookAssignmentEvaluationRecordConfiguration
    : IEntityTypeConfiguration<PlaybookAssignmentEvaluationRecord>
{
    public void Configure(EntityTypeBuilder<PlaybookAssignmentEvaluationRecord> builder)
    {
        builder.ToTable("playbook_assignment_evaluation");
        builder.HasKey(record => record.Id);
        builder.Property(record => record.Id).ValueGeneratedNever();
        builder.Property(record => record.ResultsJson).HasColumnType("jsonb").IsRequired();
        builder.HasIndex(record => new
        {
            record.OrganizationId,
            record.ClaimId,
            record.EvaluatedAt,
        });
    }
}

internal sealed class PlaybookVersionConfiguration : IEntityTypeConfiguration<PlaybookVersion>
{
    public void Configure(EntityTypeBuilder<PlaybookVersion> builder)
    {
        builder.ToTable("playbook_version");
        builder.HasKey(version => version.Id);
        builder.Property(version => version.Id).ValueGeneratedNever();
        builder.Property(version => version.WorkflowType).HasMaxLength(30).IsRequired();
        builder.Property(version => version.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(version => version.DefinitionJson).HasColumnType("jsonb").IsRequired();
        builder.Property(version => version.AssignmentRulesJson).HasColumnType("jsonb").IsRequired();
        builder.Ignore(version => version.Stages);
        builder.Ignore(version => version.AssignmentRules);
        builder.HasIndex("PlaybookId", nameof(PlaybookVersion.Version)).IsUnique();
        builder.HasIndex(version => new { version.OrganizationId, version.Status });
    }
}
