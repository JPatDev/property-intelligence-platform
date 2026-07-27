using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyIntelligence.Modules.Workflow.Domain;

namespace PropertyIntelligence.Modules.Workflow.Infrastructure.Persistence.Configurations;

internal sealed class WorkflowBlockerConfiguration : IEntityTypeConfiguration<WorkflowBlocker>
{
    public void Configure(EntityTypeBuilder<WorkflowBlocker> builder)
    {
        builder.ToTable("workflow_blocker");
        builder.HasKey(blocker => blocker.Id);

        builder.Property(blocker => blocker.Id).ValueGeneratedNever();
        builder.Property(blocker => blocker.OrganizationId).IsRequired();
        builder.Property(blocker => blocker.Code).HasMaxLength(150).IsRequired();
        builder.Property(blocker => blocker.Description).HasMaxLength(2000).IsRequired();
        builder.Property(blocker => blocker.CreatedBy).IsRequired();
        builder.Property(blocker => blocker.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(blocker => blocker.ResolvedBy);
        builder.Property(blocker => blocker.ResolvedAt).HasColumnType("timestamp with time zone");
        builder.Property(blocker => blocker.ResolutionReason).HasMaxLength(1000);
        builder.Ignore(blocker => blocker.IsResolved);

        builder.HasIndex(
            nameof(WorkflowBlocker.OrganizationId),
            "WorkflowTaskId",
            nameof(WorkflowBlocker.ResolvedAt));
    }
}
