using PropertyIntelligence.Modules.Workflow.Domain;

namespace PropertyIntelligence.Modules.Workflow.Application.Gates;

internal sealed class ClaimFieldPresentGateHandler(
    IClaimGateEvidenceReader evidenceReader,
    TimeProvider timeProvider) : GateHandlerBase(timeProvider)
{
    public override string GateType => GateTypes.ClaimFieldPresent;

    public override async Task<GateEvaluationResult> EvaluateAsync(
        GateEvaluationContext context,
        CancellationToken cancellationToken)
    {
        if (!TryGetParameter(context.Gate, "fieldName", out var fieldName))
        {
            return Indeterminate(context.Gate.Id, "Gate parameter 'fieldName' is required.");
        }

        var evidence = await evidenceReader.HasFieldValueAsync(
            context.Workflow.OrganizationId,
            context.Workflow.ClaimId,
            fieldName,
            cancellationToken);
        return FromEvidence(context.Gate.Id, evidence);
    }
}

internal sealed class DocumentExistsGateHandler(
    IDocumentGateEvidenceReader evidenceReader,
    TimeProvider timeProvider) : GateHandlerBase(timeProvider)
{
    public override string GateType => GateTypes.DocumentExists;

    public override async Task<GateEvaluationResult> EvaluateAsync(
        GateEvaluationContext context,
        CancellationToken cancellationToken)
    {
        if (!TryGetParameter(context.Gate, "documentType", out var documentType))
        {
            return Indeterminate(context.Gate.Id, "Gate parameter 'documentType' is required.");
        }

        var acceptedStatuses = context.Gate.Parameters
            .GetValueOrDefault("acceptedStatuses", "Verified")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var evidence = await evidenceReader.DocumentExistsAsync(
            context.Workflow.OrganizationId,
            context.Workflow.ClaimId,
            documentType,
            acceptedStatuses,
            cancellationToken);
        return FromEvidence(context.Gate.Id, evidence);
    }
}

internal sealed class CommunicationRecordedGateHandler(
    ICommunicationGateEvidenceReader evidenceReader,
    TimeProvider timeProvider) : GateHandlerBase(timeProvider)
{
    public override string GateType => GateTypes.CommunicationRecorded;

    public override async Task<GateEvaluationResult> EvaluateAsync(
        GateEvaluationContext context,
        CancellationToken cancellationToken)
    {
        if (!TryGetParameter(context.Gate, "communicationType", out var communicationType))
        {
            return Indeterminate(context.Gate.Id, "Gate parameter 'communicationType' is required.");
        }

        var requiredStatus = context.Gate.Parameters.GetValueOrDefault("requiredStatus", "Sent");
        var evidence = await evidenceReader.CommunicationExistsAsync(
            context.Workflow.OrganizationId,
            context.Workflow.ClaimId,
            communicationType,
            requiredStatus,
            cancellationToken);
        return FromEvidence(context.Gate.Id, evidence);
    }
}

internal sealed class ApprovalGrantedGateHandler(
    IApprovalGateEvidenceReader evidenceReader,
    TimeProvider timeProvider) : GateHandlerBase(timeProvider)
{
    public override string GateType => GateTypes.ApprovalGranted;

    public override async Task<GateEvaluationResult> EvaluateAsync(
        GateEvaluationContext context,
        CancellationToken cancellationToken)
    {
        if (!TryGetParameter(context.Gate, "approvalType", out var approvalType))
        {
            return Indeterminate(context.Gate.Id, "Gate parameter 'approvalType' is required.");
        }

        var evidence = await evidenceReader.ApprovalExistsAsync(
            context.Workflow.OrganizationId,
            context.Workflow.ClaimId,
            approvalType,
            cancellationToken);
        return FromEvidence(context.Gate.Id, evidence);
    }
}

internal sealed class TaskCompletedGateHandler(TimeProvider timeProvider) : GateHandlerBase(timeProvider)
{
    public override string GateType => GateTypes.TaskCompleted;

    public override Task<GateEvaluationResult> EvaluateAsync(
        GateEvaluationContext context,
        CancellationToken cancellationToken)
    {
        if (!TryGetParameter(context.Gate, "taskSourceDefinitionId", out var value) ||
            !Guid.TryParse(value, out var sourceDefinitionId))
        {
            return Task.FromResult(Indeterminate(
                context.Gate.Id,
                "Gate parameter 'taskSourceDefinitionId' must be a valid GUID."));
        }

        var dependency = context.Workflow.Stages
            .SelectMany(stage => stage.Tasks)
            .SingleOrDefault(task => task.SourceDefinitionId == sourceDefinitionId);

        if (dependency is null)
        {
            return Task.FromResult(Indeterminate(
                context.Gate.Id,
                "The referenced workflow task does not exist."));
        }

        var result = dependency.Status == WorkflowTaskStatus.Completed
            ? GateEvidence.Satisfied(dependency.Id.ToString(), "The required workflow task is complete.")
            : GateEvidence.Unsatisfied("The required workflow task is not complete.");
        return Task.FromResult(FromEvidence(context.Gate.Id, result));
    }
}

internal sealed class RuleSatisfiedGateHandler(
    IRuleGateEvidenceReader evidenceReader,
    TimeProvider timeProvider) : GateHandlerBase(timeProvider)
{
    public override string GateType => GateTypes.RuleSatisfied;

    public override async Task<GateEvaluationResult> EvaluateAsync(
        GateEvaluationContext context,
        CancellationToken cancellationToken)
    {
        if (!TryGetParameter(context.Gate, "ruleCode", out var ruleCode))
        {
            return Indeterminate(context.Gate.Id, "Gate parameter 'ruleCode' is required.");
        }

        var evidence = await evidenceReader.EvaluateRuleAsync(
            context.Workflow.OrganizationId,
            context.Workflow.ClaimId,
            ruleCode,
            context.Gate.Parameters,
            cancellationToken);
        return FromEvidence(context.Gate.Id, evidence);
    }
}
