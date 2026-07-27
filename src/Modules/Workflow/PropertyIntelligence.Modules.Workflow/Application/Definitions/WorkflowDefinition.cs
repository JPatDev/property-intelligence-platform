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
    WorkflowDefinition? Find(string key, int version);

    IReadOnlyList<WorkflowDefinitionSummary> List();
}
