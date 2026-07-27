using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyIntelligence.Modules.Workflow.Domain;

namespace PropertyIntelligence.Modules.Workflow.Infrastructure.Persistence.Configurations;

internal sealed class WorkflowTaskConfiguration : IEntityTypeConfiguration<WorkflowTask>
{
    public void Configure(EntityTypeBuilder<WorkflowTask> builder)
    {
        builder.ToTable("workflow_task");
        builder.HasKey(task => task.Id);

        builder.Property(task => task.Id).ValueGeneratedNever();
        builder.Property(task => task.OrganizationId).IsRequired();
        builder.Property(task => task.SourceDefinitionId).IsRequired();
        builder.Property(task => task.Name).HasMaxLength(300).IsRequired();
        builder.Property(task => task.Order).HasColumnName("task_order").IsRequired();
        builder.Property(task => task.Priority).IsRequired();
        builder.Property(task => task.IsRequired).IsRequired();
        builder.Property(task => task.DueAt).HasColumnType("timestamp with time zone");
        builder.Property(task => task.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(task => task.AssignedTo);
        builder.Property(task => task.StartedAt).HasColumnType("timestamp with time zone");
        builder.Property(task => task.CompletedBy);
        builder.Property(task => task.CompletedAt).HasColumnType("timestamp with time zone");
        builder.Property(task => task.CancelledBy);
        builder.Property(task => task.CancelledAt).HasColumnType("timestamp with time zone");
        builder.Property(task => task.CancellationReason).HasMaxLength(1000);

        builder.Ignore(task => task.DependencySourceDefinitionIds);
        builder.Ignore(task => task.CompletionGates);
        builder.Ignore(task => task.Blockers);
        builder.Ignore(task => task.HasOpenBlockers);

        builder.HasMany<WorkflowTaskDependency>("_dependencies")
            .WithOne()
            .HasForeignKey(dependency => dependency.TaskId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany<CompletionGateDefinition>("_completionGates")
            .WithOne()
            .HasForeignKey("WorkflowTaskId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany<WorkflowBlocker>("_blockers")
            .WithOne()
            .HasForeignKey("WorkflowTaskId")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation("_dependencies").UsePropertyAccessMode(PropertyAccessMode.Field).AutoInclude();
        builder.Navigation("_completionGates").UsePropertyAccessMode(PropertyAccessMode.Field).AutoInclude();
        builder.Navigation("_blockers").UsePropertyAccessMode(PropertyAccessMode.Field).AutoInclude();

        builder.HasIndex("WorkflowStageId", nameof(WorkflowTask.Order)).IsUnique();
        builder.HasIndex("WorkflowStageId", nameof(WorkflowTask.SourceDefinitionId)).IsUnique();
        builder.HasIndex(task => new { task.OrganizationId, task.Status, task.AssignedTo });
        builder.HasIndex(task => new { task.OrganizationId, task.Status, task.DueAt });
    }
}
