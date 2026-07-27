namespace PropertyIntelligence.Modules.Workflow.Application.Errors;

public abstract class WorkflowApplicationException(
    string code,
    string message,
    Exception? innerException = null) : Exception(message, innerException)
{
    public string Code { get; } = code;
}

public sealed class WorkflowNotFoundException(Guid workflowId)
    : WorkflowApplicationException(
        "workflow.not_found",
        $"Workflow '{workflowId}' was not found.");

public sealed class WorkflowEscalationNotFoundException(Guid escalationId)
    : WorkflowApplicationException(
        "workflow.escalation_not_found",
        $"Workflow escalation '{escalationId}' was not found.");

public sealed class WorkflowDefinitionNotFoundException(string key, int version)
    : WorkflowApplicationException(
        "workflow.definition_not_found",
        $"Workflow definition '{key}' version {version} was not found.");

public sealed class WorkflowDefinitionTypeMismatchException(
    string key,
    int version,
    Domain.WorkflowType requestedType,
    Domain.WorkflowType definitionType)
    : WorkflowApplicationException(
        "workflow.definition_type_mismatch",
        $"Workflow definition '{key}' version {version} is {definitionType}, not {requestedType}.");

public sealed class WorkflowConcurrencyException(Exception? innerException = null)
    : WorkflowApplicationException(
        "workflow.concurrency_conflict",
        "The workflow was changed by another operation. Reload it and retry.",
        innerException);

public sealed class WorkflowValidationException(IReadOnlyDictionary<string, string[]> errors)
    : WorkflowApplicationException(
        "workflow.validation_failed",
        "One or more workflow request values are invalid.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}
