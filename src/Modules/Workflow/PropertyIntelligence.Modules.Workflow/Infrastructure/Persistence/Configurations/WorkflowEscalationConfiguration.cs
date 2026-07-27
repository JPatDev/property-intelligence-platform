using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyIntelligence.Modules.Workflow.Domain;

namespace PropertyIntelligence.Modules.Workflow.Infrastructure.Persistence.Configurations;

internal sealed class WorkflowEscalationConfiguration : IEntityTypeConfiguration<WorkflowEscalation>
{
    public void Configure(EntityTypeBuilder<WorkflowEscalation> builder)
    {
        builder.ToTable("workflow_escalation");
        builder.HasKey(escalation => escalation.Id);

        builder.Property(escalation => escalation.Id).ValueGeneratedNever();
        builder.Property(escalation => escalation.OrganizationId).IsRequired();
        builder.Property(escalation => escalation.WorkflowId).IsRequired();
        builder.Property(escalation => escalation.TaskId);
        builder.Property(escalation => escalation.Type).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(escalation => escalation.Severity).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(escalation => escalation.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(escalation => escalation.DeduplicationKey).HasMaxLength(500).IsRequired();
        builder.Property(escalation => escalation.Message).HasMaxLength(1000).IsRequired();
        builder.Property(escalation => escalation.TriggeredAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(escalation => escalation.LastObservedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(escalation => escalation.AcknowledgedAt).HasColumnType("timestamp with time zone");
        builder.Property(escalation => escalation.AcknowledgedBy);
        builder.Property(escalation => escalation.ResolvedAt).HasColumnType("timestamp with time zone");
        builder.Property(escalation => escalation.ResolutionReason).HasMaxLength(1000);

        builder.HasOne<WorkflowInstance>()
            .WithMany()
            .HasForeignKey(escalation => escalation.WorkflowId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(escalation => new
        {
            escalation.OrganizationId,
            escalation.DeduplicationKey,
        }).IsUnique();
        builder.HasIndex(escalation => new
        {
            escalation.OrganizationId,
            escalation.Status,
            escalation.Severity,
        });
        builder.HasIndex(escalation => new { escalation.OrganizationId, escalation.TaskId });
    }
}
