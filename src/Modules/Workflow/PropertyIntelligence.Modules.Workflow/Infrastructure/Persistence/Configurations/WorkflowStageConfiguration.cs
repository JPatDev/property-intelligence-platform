using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyIntelligence.Modules.Workflow.Domain;

namespace PropertyIntelligence.Modules.Workflow.Infrastructure.Persistence.Configurations;

internal sealed class WorkflowStageConfiguration : IEntityTypeConfiguration<WorkflowStage>
{
    public void Configure(EntityTypeBuilder<WorkflowStage> builder)
    {
        builder.ToTable("workflow_stage");
        builder.HasKey(stage => stage.Id);

        builder.Property(stage => stage.Id).ValueGeneratedNever();
        builder.Property(stage => stage.OrganizationId).IsRequired();
        builder.Property(stage => stage.SourceDefinitionId).IsRequired();
        builder.Property(stage => stage.Name).HasMaxLength(300).IsRequired();
        builder.Property(stage => stage.Order).HasColumnName("stage_order").IsRequired();
        builder.Property(stage => stage.IsOptional).IsRequired();
        builder.Property(stage => stage.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(stage => stage.ActivatedAt).HasColumnType("timestamp with time zone");
        builder.Property(stage => stage.CompletedAt).HasColumnType("timestamp with time zone");
        builder.Property(stage => stage.SkippedBy);
        builder.Property(stage => stage.SkippedAt).HasColumnType("timestamp with time zone");
        builder.Property(stage => stage.SkipReason).HasMaxLength(1000);

        builder.HasMany(stage => stage.Tasks)
            .WithOne()
            .HasForeignKey("WorkflowStageId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(stage => stage.Tasks)
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .AutoInclude();

        builder.HasIndex("WorkflowInstanceId", nameof(WorkflowStage.Order)).IsUnique();
        builder.HasIndex("WorkflowInstanceId", nameof(WorkflowStage.SourceDefinitionId)).IsUnique();
        builder.HasIndex(stage => new { stage.OrganizationId, stage.Status });
    }
}
