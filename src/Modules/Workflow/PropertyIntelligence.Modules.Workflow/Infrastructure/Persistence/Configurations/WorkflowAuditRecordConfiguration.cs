using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyIntelligence.Modules.Workflow.Domain;

namespace PropertyIntelligence.Modules.Workflow.Infrastructure.Persistence.Configurations;

internal sealed class WorkflowAuditRecordConfiguration : IEntityTypeConfiguration<WorkflowAuditRecord>
{
    public void Configure(EntityTypeBuilder<WorkflowAuditRecord> builder)
    {
        builder.ToTable("workflow_audit");
        builder.HasKey(audit => audit.Id);

        builder.Property(audit => audit.Id).ValueGeneratedNever();
        builder.Property(audit => audit.OrganizationId).IsRequired();
        builder.Property(audit => audit.WorkflowId).IsRequired();
        builder.Property(audit => audit.EntityType).HasMaxLength(150).IsRequired();
        builder.Property(audit => audit.EntityId).IsRequired();
        builder.Property(audit => audit.ActorType).HasMaxLength(50).IsRequired();
        builder.Property(audit => audit.ActorId);
        builder.Property(audit => audit.Action).HasMaxLength(200).IsRequired();
        builder.Property(audit => audit.PreviousState).HasColumnType("jsonb");
        builder.Property(audit => audit.NewState).HasColumnType("jsonb");
        builder.Property(audit => audit.OccurredAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(audit => audit.CorrelationId).HasMaxLength(100).IsRequired();
        builder.Property(audit => audit.CausationId).HasMaxLength(200).IsRequired();
        builder.Property(audit => audit.SystemGenerated).IsRequired();

        builder.HasOne<WorkflowInstance>()
            .WithMany()
            .HasForeignKey(audit => audit.WorkflowId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(audit => new
        {
            audit.OrganizationId,
            audit.WorkflowId,
            audit.OccurredAt,
        });
        builder.HasIndex(audit => new { audit.OrganizationId, audit.CorrelationId });
    }
}
