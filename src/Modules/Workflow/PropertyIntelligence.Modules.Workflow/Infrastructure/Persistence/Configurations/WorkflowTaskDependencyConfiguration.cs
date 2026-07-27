using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyIntelligence.Modules.Workflow.Domain;

namespace PropertyIntelligence.Modules.Workflow.Infrastructure.Persistence.Configurations;

internal sealed class WorkflowTaskDependencyConfiguration : IEntityTypeConfiguration<WorkflowTaskDependency>
{
    public void Configure(EntityTypeBuilder<WorkflowTaskDependency> builder)
    {
        builder.ToTable("workflow_task_dependency");
        builder.HasKey(dependency => new
        {
            dependency.TaskId,
            dependency.DependencySourceDefinitionId,
        });

        builder.Property(dependency => dependency.TaskId).ValueGeneratedNever();
        builder.Property(dependency => dependency.OrganizationId).IsRequired();
        builder.Property(dependency => dependency.DependencySourceDefinitionId).ValueGeneratedNever();
        builder.HasIndex(dependency => new
        {
            dependency.OrganizationId,
            dependency.DependencySourceDefinitionId,
        });
    }
}
