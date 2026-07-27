namespace PropertyIntelligence.Modules.Workflow.Domain;

internal sealed class WorkflowTaskDependency
{
    private WorkflowTaskDependency()
    {
    }

    internal WorkflowTaskDependency(Guid organizationId, Guid taskId, Guid dependencySourceDefinitionId)
    {
        OrganizationId = organizationId;
        TaskId = taskId;
        DependencySourceDefinitionId = dependencySourceDefinitionId;
    }

    public Guid OrganizationId { get; private set; }
    public Guid TaskId { get; private set; }
    public Guid DependencySourceDefinitionId { get; private set; }
}
