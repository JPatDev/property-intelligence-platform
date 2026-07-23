namespace PropertyIntelligence.Modules.Workflow.Domain;

public sealed class WorkflowDomainException(string code, string message) : InvalidOperationException(message)
{
    public string Code { get; } = code;
}
