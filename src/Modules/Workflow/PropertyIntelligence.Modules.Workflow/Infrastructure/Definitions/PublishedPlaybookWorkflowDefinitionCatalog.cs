using PropertyIntelligence.Modules.Playbooks.Contracts;
using PropertyIntelligence.Modules.Workflow.Application.Definitions;
using PropertyIntelligence.Modules.Workflow.Domain;

namespace PropertyIntelligence.Modules.Workflow.Infrastructure.Definitions;

internal sealed class PublishedPlaybookWorkflowDefinitionCatalog(
    IPublishedPlaybookReader playbooks) : IWorkflowDefinitionCatalog
{
    public async Task<WorkflowDefinition?> FindAsync(
        Guid organizationId,
        string key,
        int version,
        CancellationToken cancellationToken)
    {
        var playbook = await playbooks.FindAsync(
            organizationId,
            key,
            version,
            cancellationToken);
        return playbook is null ? null : ToWorkflowDefinition(playbook);
    }

    public async Task<IReadOnlyList<WorkflowDefinitionSummary>> ListAsync(
        Guid organizationId,
        CancellationToken cancellationToken) =>
        (await playbooks.ListAsync(organizationId, cancellationToken))
        .Select(playbook => new WorkflowDefinitionSummary(
            playbook.Key,
            playbook.Version,
            playbook.Name,
            playbook.Description,
            ParseWorkflowType(playbook.WorkflowType)))
        .ToArray();

    private static WorkflowDefinition ToWorkflowDefinition(PublishedPlaybookDefinition playbook) =>
        new(
            playbook.Key,
            playbook.Version,
            playbook.Name,
            playbook.Description,
            ParseWorkflowType(playbook.WorkflowType),
            new WorkflowSnapshot(
                playbook.PlaybookId,
                playbook.VersionId,
                playbook.SchemaVersion,
                playbook.Stages.Select(stage => new StageSnapshot(
                    stage.Id,
                    stage.Name,
                    stage.Order,
                    stage.IsOptional,
                    stage.Tasks.Select(task => new TaskSnapshot(
                        task.Id,
                        task.Name,
                        task.Order,
                        task.Priority,
                        task.IsRequired,
                        task.DependencyIds.ToHashSet(),
                        task.CompletionGates.Select(gate => new CompletionGateDefinition(
                            gate.Id,
                            gate.GateType,
                            ParseScope(gate.Scope),
                            ParseSeverity(gate.Severity),
                            gate.Parameters,
                            gate.FailureCode,
                            gate.FailureMessage,
                            gate.EvaluationVersion)).ToArray()))
                    .ToArray()))
                .ToArray()));

    private static WorkflowType ParseWorkflowType(string value) =>
        Enum.TryParse<WorkflowType>(value, true, out var parsed)
            ? parsed
            : throw InvalidDefinition("workflow type", value);

    private static CompletionGateScope ParseScope(string value) =>
        Enum.TryParse<CompletionGateScope>(value, true, out var parsed)
            ? parsed
            : throw InvalidDefinition("gate scope", value);

    private static CompletionGateSeverity ParseSeverity(string value) =>
        Enum.TryParse<CompletionGateSeverity>(value, true, out var parsed)
            ? parsed
            : throw InvalidDefinition("gate severity", value);

    private static InvalidOperationException InvalidDefinition(string field, string value) =>
        new($"Published playbook contains unsupported {field} '{value}'.");
}
