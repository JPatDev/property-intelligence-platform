using System.Collections.ObjectModel;

namespace PropertyIntelligence.Modules.Workflow.Domain;

public sealed class WorkflowTask
{
    private readonly List<WorkflowBlocker> _blockers = [];
    private readonly List<WorkflowTaskDependency> _dependencies = [];
    private readonly List<CompletionGateDefinition> _completionGates = [];

    private WorkflowTask()
    {
    }

    internal WorkflowTask(Guid organizationId, TaskSnapshot snapshot)
    {
        Id = Guid.NewGuid();
        OrganizationId = organizationId;
        SourceDefinitionId = snapshot.SourceDefinitionId;
        Name = snapshot.Name.Trim();
        Order = snapshot.Order;
        Priority = snapshot.Priority;
        IsRequired = snapshot.IsRequired;
        DueAt = snapshot.DueAt;
        _dependencies.AddRange(
            snapshot.DependencySourceDefinitionIds.Select(dependencyId =>
                new WorkflowTaskDependency(organizationId, Id, dependencyId)));
        _completionGates.AddRange(snapshot.CompletionGates.Select(gate => gate.Freeze(organizationId)));
        Status = WorkflowTaskStatus.Pending;
    }

    public Guid Id { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid SourceDefinitionId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int Order { get; private set; }
    public int Priority { get; private set; }
    public bool IsRequired { get; private set; }
    public DateTimeOffset? DueAt { get; private set; }
    public WorkflowTaskStatus Status { get; private set; }
    public Guid? AssignedTo { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public Guid? CompletedBy { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public Guid? CancelledBy { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }
    public string? CancellationReason { get; private set; }
    public IReadOnlySet<Guid> DependencySourceDefinitionIds =>
        _dependencies.Select(dependency => dependency.DependencySourceDefinitionId).ToHashSet();
    public IReadOnlyList<CompletionGateDefinition> CompletionGates =>
        new ReadOnlyCollection<CompletionGateDefinition>(_completionGates);
    public IReadOnlyList<WorkflowBlocker> Blockers => _blockers.AsReadOnly();
    public bool HasOpenBlockers => _blockers.Any(blocker => !blocker.IsResolved);

    internal void Assign(Guid assigneeId)
    {
        EnsureMutable();
        if (assigneeId == Guid.Empty)
        {
            throw new WorkflowDomainException("workflow.assignee_required", "An assignee ID is required.");
        }

        AssignedTo = assigneeId;
        if (Status == WorkflowTaskStatus.Pending)
        {
            Status = WorkflowTaskStatus.Assigned;
        }
    }

    internal void Start(DateTimeOffset startedAt)
    {
        EnsureMutable();
        if (Status is not (WorkflowTaskStatus.Assigned or WorkflowTaskStatus.Pending))
        {
            throw InvalidTransition(WorkflowTaskStatus.InProgress);
        }

        if (HasOpenBlockers)
        {
            throw new WorkflowDomainException("workflow.task_has_blockers", "A blocked task cannot be started.");
        }

        Status = WorkflowTaskStatus.InProgress;
        StartedAt ??= startedAt;
    }

    internal WorkflowBlocker AddBlocker(string code, string description, Guid actorId, DateTimeOffset createdAt)
    {
        EnsureMutable();
        if (actorId == Guid.Empty)
        {
            throw new WorkflowDomainException("workflow.actor_required", "An actor ID is required.");
        }

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(description))
        {
            throw new WorkflowDomainException(
                "workflow.blocker_details_required",
                "A blocker code and description are required.");
        }

        var blocker = new WorkflowBlocker(
            Guid.NewGuid(),
            OrganizationId,
            code.Trim(),
            description.Trim(),
            actorId,
            createdAt);
        _blockers.Add(blocker);
        Status = WorkflowTaskStatus.Blocked;
        return blocker;
    }

    internal void ResolveBlocker(Guid blockerId, Guid actorId, string reason, DateTimeOffset resolvedAt)
    {
        var blocker = _blockers.SingleOrDefault(candidate => candidate.Id == blockerId)
            ?? throw new WorkflowDomainException("workflow.blocker_not_found", "The blocker was not found on the task.");

        blocker.Resolve(actorId, reason, resolvedAt);
        if (!HasOpenBlockers && Status == WorkflowTaskStatus.Blocked)
        {
            Status = StartedAt.HasValue ? WorkflowTaskStatus.InProgress :
                AssignedTo.HasValue ? WorkflowTaskStatus.Assigned : WorkflowTaskStatus.Pending;
        }
    }

    internal TaskCompletionResult Complete(
        Guid actorId,
        DateTimeOffset completedAt,
        IReadOnlyCollection<GateEvaluationResult> evaluations)
    {
        EnsureMutable();
        if (actorId == Guid.Empty)
        {
            throw new WorkflowDomainException("workflow.actor_required", "An actor ID is required.");
        }

        if (Status is not (WorkflowTaskStatus.Assigned or WorkflowTaskStatus.InProgress))
        {
            throw InvalidTransition(WorkflowTaskStatus.Completed);
        }

        if (HasOpenBlockers)
        {
            throw new WorkflowDomainException("workflow.task_has_blockers", "A task with open blockers cannot be completed.");
        }

        var resultsByGate = evaluations
            .GroupBy(result => result.GateId)
            .ToDictionary(group => group.Key, group => group.OrderByDescending(result => result.EvaluatedAt).First());

        var failures = CompletionGates
            .Where(gate => gate.Severity == CompletionGateSeverity.Required)
            .Select(gate => (Gate: gate, Result: resultsByGate.GetValueOrDefault(gate.Id)))
            .Where(item => item.Result?.Outcome != GateEvaluationOutcome.Passed)
            .Select(item => new CompletionFailure(
                item.Gate.FailureCode,
                item.Gate.FailureMessage,
                item.Result?.Outcome ?? GateEvaluationOutcome.Indeterminate))
            .ToArray();

        if (failures.Length > 0)
        {
            return new TaskCompletionResult(false, failures);
        }

        Status = WorkflowTaskStatus.Completed;
        CompletedBy = actorId;
        CompletedAt = completedAt;
        return TaskCompletionResult.Success;
    }

    internal void Cancel(Guid actorId, string reason, DateTimeOffset cancelledAt)
    {
        EnsureMutable();
        if (actorId == Guid.Empty)
        {
            throw new WorkflowDomainException("workflow.actor_required", "An actor ID is required.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new WorkflowDomainException("workflow.reason_required", "A cancellation reason is required.");
        }

        Status = WorkflowTaskStatus.Cancelled;
        CancelledBy = actorId;
        CancelledAt = cancelledAt;
        CancellationReason = reason.Trim();
    }

    private void EnsureMutable()
    {
        if (Status is WorkflowTaskStatus.Completed or WorkflowTaskStatus.Cancelled)
        {
            throw new WorkflowDomainException("workflow.task_terminal", "A completed or cancelled task cannot be changed.");
        }
    }

    private WorkflowDomainException InvalidTransition(WorkflowTaskStatus target) =>
        new(
            "workflow.task_transition_invalid",
            $"Task status cannot transition from {Status} to {target}.");
}
