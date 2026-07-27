using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyIntelligence.Modules.Workflow.Domain;

namespace PropertyIntelligence.Modules.Workflow.Infrastructure.Persistence.Configurations;

internal sealed class WorkflowInstanceConfiguration : IEntityTypeConfiguration<WorkflowInstance>
{
    public void Configure(EntityTypeBuilder<WorkflowInstance> builder)
    {
        builder.ToTable("workflow_instance");
        builder.HasKey(workflow => workflow.Id);

        builder.Property(workflow => workflow.Id).ValueGeneratedNever();
        builder.Property(workflow => workflow.OrganizationId).IsRequired();
        builder.Property(workflow => workflow.ClaimId).IsRequired();
        builder.Property(workflow => workflow.Type).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(workflow => workflow.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(workflow => workflow.SourcePlaybookId).IsRequired();
        builder.Property(workflow => workflow.SourcePlaybookVersionId).IsRequired();
        builder.Property(workflow => workflow.SnapshotSchemaVersion).IsRequired();
        builder.Property(workflow => workflow.Version).IsConcurrencyToken().IsRequired();
        builder.Property(workflow => workflow.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(workflow => workflow.StartedAt).HasColumnType("timestamp with time zone");
        builder.Property(workflow => workflow.CompletedAt).HasColumnType("timestamp with time zone");
        builder.Property(workflow => workflow.CancelledAt).HasColumnType("timestamp with time zone");
        builder.Property(workflow => workflow.CancelledBy);
        builder.Property(workflow => workflow.CancellationReason).HasMaxLength(1000);
        builder.Property(workflow => workflow.ArchivedAt).HasColumnType("timestamp with time zone");
        builder.Ignore(workflow => workflow.CurrentStages);

        builder.HasMany(workflow => workflow.Stages)
            .WithOne()
            .HasForeignKey("WorkflowInstanceId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(workflow => workflow.Stages)
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .AutoInclude();

        builder.HasIndex(workflow => new { workflow.OrganizationId, workflow.ClaimId });
        builder.HasIndex(workflow => new { workflow.OrganizationId, workflow.Status });
        builder.HasIndex(workflow => new
        {
            workflow.OrganizationId,
            workflow.SourcePlaybookVersionId,
        });
    }
}
