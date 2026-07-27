using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyIntelligence.Modules.Workflow.Domain;

namespace PropertyIntelligence.Modules.Workflow.Infrastructure.Persistence.Configurations;

internal sealed class NextActionSnapshotConfiguration : IEntityTypeConfiguration<NextActionSnapshot>
{
    public void Configure(EntityTypeBuilder<NextActionSnapshot> builder)
    {
        builder.ToTable("next_action");
        builder.HasKey(action => action.WorkflowId);

        builder.Property(action => action.WorkflowId).ValueGeneratedNever();
        builder.Property(action => action.OrganizationId).IsRequired();
        builder.Property(action => action.TaskId).IsRequired();
        builder.Property(action => action.Action).HasMaxLength(500).IsRequired();
        builder.Property(action => action.OwnerId);
        builder.Property(action => action.DueAt).HasColumnType("timestamp with time zone");
        builder.Property(action => action.CalculatedPriority).IsRequired();
        builder.Property(action => action.ReasonCode).HasMaxLength(100).IsRequired();
        builder.Property(action => action.Reason).HasMaxLength(1000).IsRequired();
        builder.Property(action => action.CalculatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(action => action.CalculationVersion).IsRequired();

        builder.HasOne<WorkflowInstance>()
            .WithOne()
            .HasForeignKey<NextActionSnapshot>(action => action.WorkflowId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(action => new
        {
            action.OrganizationId,
            action.OwnerId,
            action.CalculatedPriority,
        });
        builder.HasIndex(action => new { action.OrganizationId, action.DueAt });
    }
}
