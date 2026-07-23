namespace PropertyIntelligence.Modules.Workflow.Domain;

public enum WorkflowType
{
    Primary = 1,
    Supplemental = 2,
}

public enum WorkflowStatus
{
    NotStarted = 1,
    Active = 2,
    Blocked = 3,
    Completed = 4,
    Cancelled = 5,
    Archived = 6,
}

public enum WorkflowStageStatus
{
    Pending = 1,
    Active = 2,
    Blocked = 3,
    Completed = 4,
    Skipped = 5,
    Cancelled = 6,
}

public enum WorkflowTaskStatus
{
    Pending = 1,
    Assigned = 2,
    InProgress = 3,
    Blocked = 4,
    Completed = 5,
    Cancelled = 6,
}

public enum CompletionGateScope
{
    Task = 1,
    Stage = 2,
    Workflow = 3,
}

public enum CompletionGateSeverity
{
    Advisory = 1,
    Required = 2,
}

public enum GateEvaluationOutcome
{
    Passed = 1,
    Failed = 2,
    Indeterminate = 3,
}
