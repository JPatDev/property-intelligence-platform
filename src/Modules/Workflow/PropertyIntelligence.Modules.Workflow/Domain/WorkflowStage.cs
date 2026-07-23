namespace PropertyIntelligence.Modules.Workflow.Domain;

public sealed class WorkflowStage
{
    private readonly List<WorkflowTask> _tasks;

    internal WorkflowStage(StageSnapshot snapshot)
    {
        Id = Guid.NewGuid();
        SourceDefinitionId = snapshot.SourceDefinitionId;
        Name = snapshot.Name.Trim();
        Order = snapshot.Order;
        IsOptional = snapshot.IsOptional;
        Status = WorkflowStageStatus.Pending;
        _tasks = snapshot.Tasks.OrderBy(task => task.Order).Select(task => new WorkflowTask(task)).ToList();
    }

    public Guid Id { get; }
    public Guid SourceDefinitionId { get; }
    public string Name { get; }
    public int Order { get; }
    public bool IsOptional { get; }
    public WorkflowStageStatus Status { get; private set; }
    public DateTimeOffset? ActivatedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public Guid? SkippedBy { get; private set; }
    public DateTimeOffset? SkippedAt { get; private set; }
    public string? SkipReason { get; private set; }
    public IReadOnlyList<WorkflowTask> Tasks => _tasks.AsReadOnly();

    internal void Activate(DateTimeOffset activatedAt)
    {
        if (Status != WorkflowStageStatus.Pending)
        {
            throw new WorkflowDomainException(
                "workflow.stage_transition_invalid",
                $"Stage status cannot transition from {Status} to {WorkflowStageStatus.Active}.");
        }

        Status = WorkflowStageStatus.Active;
        ActivatedAt = activatedAt;
    }

    internal bool TryComplete(DateTimeOffset completedAt)
    {
        if (Status is not (WorkflowStageStatus.Active or WorkflowStageStatus.Blocked))
        {
            return false;
        }

        var requiredTasksFinished = _tasks
            .Where(task => task.IsRequired)
            .All(task => task.Status == WorkflowTaskStatus.Completed);

        if (!requiredTasksFinished)
        {
            return false;
        }

        Status = WorkflowStageStatus.Completed;
        CompletedAt = completedAt;
        return true;
    }

    internal void RefreshBlockedStatus()
    {
        if (Status is not (WorkflowStageStatus.Active or WorkflowStageStatus.Blocked))
        {
            return;
        }

        var unfinishedRequiredTasks = _tasks
            .Where(task => task.IsRequired && task.Status != WorkflowTaskStatus.Completed)
            .ToArray();

        Status = unfinishedRequiredTasks.Length > 0 &&
                 unfinishedRequiredTasks.All(task => task.Status == WorkflowTaskStatus.Blocked)
            ? WorkflowStageStatus.Blocked
            : WorkflowStageStatus.Active;
    }

    internal void Skip(Guid actorId, string reason, DateTimeOffset skippedAt)
    {
        if (!IsOptional)
        {
            throw new WorkflowDomainException("workflow.stage_required", "A required stage cannot be skipped.");
        }

        if (Status != WorkflowStageStatus.Pending)
        {
            throw new WorkflowDomainException(
                "workflow.stage_transition_invalid",
                $"Stage status cannot transition from {Status} to {WorkflowStageStatus.Skipped}.");
        }

        if (actorId == Guid.Empty)
        {
            throw new WorkflowDomainException("workflow.actor_required", "An actor ID is required.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new WorkflowDomainException("workflow.reason_required", "A skip reason is required.");
        }

        Status = WorkflowStageStatus.Skipped;
        SkippedBy = actorId;
        SkippedAt = skippedAt;
        SkipReason = reason.Trim();
    }

    internal void CancelTasks(Guid actorId, string reason, DateTimeOffset cancelledAt)
    {
        foreach (var task in _tasks.Where(task =>
                     task.Status is not (WorkflowTaskStatus.Completed or WorkflowTaskStatus.Cancelled)))
        {
            task.Cancel(actorId, reason, cancelledAt);
        }

        Status = WorkflowStageStatus.Cancelled;
    }
}
