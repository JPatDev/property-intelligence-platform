using PropertyIntelligence.Modules.Workflow.Domain;

namespace PropertyIntelligence.Modules.Workflow.Application.Definitions;

public sealed record WorkflowDefinition(
    string Key,
    int Version,
    string Name,
    string Description,
    WorkflowType Type,
    WorkflowSnapshot Snapshot);

public sealed record WorkflowDefinitionSummary(
    string Key,
    int Version,
    string Name,
    string Description,
    WorkflowType Type);

public interface IWorkflowDefinitionCatalog
{
    Task<WorkflowDefinition?> FindAsync(
        Guid organizationId,
        string key,
        int version,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<WorkflowDefinitionSummary>> ListAsync(
        Guid organizationId,
        CancellationToken cancellationToken);
}
