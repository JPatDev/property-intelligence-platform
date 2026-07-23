using System.Collections.ObjectModel;

namespace PropertyIntelligence.Modules.Workflow.Domain;

public sealed class WorkflowInstance
{
    private readonly List<WorkflowStage> _stages;

    private WorkflowInstance(
        Guid organizationId,
        Guid claimId,
        WorkflowType type,
        WorkflowSnapshot snapshot,
        DateTimeOffset createdAt)
    {
        Id = Guid.NewGuid();
        OrganizationId = organizationId;
        ClaimId = claimId;
        Type = type;
        SourcePlaybookId = snapshot.SourcePlaybookId;
        SourcePlaybookVersionId = snapshot.SourcePlaybookVersionId;
        SnapshotSchemaVersion = snapshot.SchemaVersion;
        CreatedAt = createdAt;
        Status = WorkflowStatus.NotStarted;
        Version = 1;
        _stages = snapshot.Stages.Select(stage => new WorkflowStage(stage)).ToList();
    }

    public Guid Id { get; }
    public Guid OrganizationId { get; }
    public Guid ClaimId { get; }
    public WorkflowType Type { get; }
    public Guid SourcePlaybookId { get; }
    public Guid SourcePlaybookVersionId { get; }
    public int SnapshotSchemaVersion { get; }
    public WorkflowStatus Status { get; private set; }
    public long Version { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }
    public Guid? CancelledBy { get; private set; }
    public string? CancellationReason { get; private set; }
    public DateTimeOffset? ArchivedAt { get; private set; }
    public IReadOnlyList<WorkflowStage> Stages => new ReadOnlyCollection<WorkflowStage>(_stages);
    public IReadOnlyList<WorkflowStage> CurrentStages =>
        _stages.Where(stage => stage.Status is WorkflowStageStatus.Active or WorkflowStageStatus.Blocked).ToArray();

    public static WorkflowInstance Create(
        Guid organizationId,
        Guid claimId,
        WorkflowType type,
        WorkflowSnapshot snapshot,
        DateTimeOffset createdAt)
    {
        if (organizationId == Guid.Empty)
        {
            throw new WorkflowDomainException("workflow.organization_id_required", "An organization ID is required.");
        }

        if (claimId == Guid.Empty)
        {
            throw new WorkflowDomainException("workflow.claim_id_required", "A claim ID is required.");
        }

        ArgumentNullException.ThrowIfNull(snapshot);
        var validatedSnapshot = snapshot.Validate();
        return new WorkflowInstance(organizationId, claimId, type, validatedSnapshot, createdAt);
    }

    public void Start(DateTimeOffset startedAt)
    {
        if (Status != WorkflowStatus.NotStarted)
        {
            throw InvalidTransition(WorkflowStatus.Active);
        }

        Status = WorkflowStatus.Active;
        StartedAt = startedAt;
        ActivateNextStage(startedAt);
        IncrementVersion();
    }

    public void AssignTask(Guid taskId, Guid assigneeId)
    {
        EnsureOperational();
        FindTask(taskId).Assign(assigneeId);
        IncrementVersion();
    }

    public void StartTask(Guid taskId, DateTimeOffset startedAt)
    {
        EnsureOperational();
        var task = FindTask(taskId);
        EnsureTaskStageActive(task);
        EnsureDependenciesComplete(task);
        task.Start(startedAt);
        RefreshWorkflowStatus();
        IncrementVersion();
    }

    public TaskCompletionResult CompleteTask(
        Guid taskId,
        Guid actorId,
        DateTimeOffset completedAt,
        IReadOnlyCollection<GateEvaluationResult> evaluations)
    {
        EnsureOperational();
        var task = FindTask(taskId);
        EnsureTaskStageActive(task);
        EnsureDependenciesComplete(task);

        var result = task.Complete(actorId, completedAt, evaluations);
        if (!result.Succeeded)
        {
            return result;
        }

        var stage = FindStageContaining(task);
        if (stage.TryComplete(completedAt))
        {
            ActivateNextStage(completedAt);
        }

        RefreshWorkflowStatus(completedAt);
        IncrementVersion();
        return result;
    }

    public Guid AddTaskBlocker(
        Guid taskId,
        string code,
        string description,
        Guid actorId,
        DateTimeOffset createdAt)
    {
        EnsureOperational();
        var task = FindTask(taskId);
        var blocker = task.AddBlocker(code, description, actorId, createdAt);
        FindStageContaining(task).RefreshBlockedStatus();
        RefreshWorkflowStatus();
        IncrementVersion();
        return blocker.Id;
    }

    public void ResolveTaskBlocker(
        Guid taskId,
        Guid blockerId,
        Guid actorId,
        string reason,
        DateTimeOffset resolvedAt)
    {
        EnsureOperational();
        var task = FindTask(taskId);
        task.ResolveBlocker(blockerId, actorId, reason, resolvedAt);
        FindStageContaining(task).RefreshBlockedStatus();
        RefreshWorkflowStatus();
        IncrementVersion();
    }

    public void SkipStage(Guid stageId, Guid actorId, string reason, DateTimeOffset skippedAt)
    {
        EnsureOperational();
        var stage = FindStage(stageId);
        stage.Skip(actorId, reason, skippedAt);
        ActivateNextStage(skippedAt);
        RefreshWorkflowStatus(skippedAt);
        IncrementVersion();
    }

    public void Cancel(Guid actorId, string reason, DateTimeOffset cancelledAt)
    {
        if (Status is WorkflowStatus.Completed or WorkflowStatus.Cancelled or WorkflowStatus.Archived)
        {
            throw InvalidTransition(WorkflowStatus.Cancelled);
        }

        if (actorId == Guid.Empty)
        {
            throw new WorkflowDomainException("workflow.actor_required", "An actor ID is required.");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new WorkflowDomainException("workflow.reason_required", "A cancellation reason is required.");
        }

        foreach (var stage in _stages.Where(stage =>
                     stage.Status is not (WorkflowStageStatus.Completed or WorkflowStageStatus.Skipped or
                         WorkflowStageStatus.Cancelled)))
        {
            stage.CancelTasks(actorId, reason, cancelledAt);
        }

        Status = WorkflowStatus.Cancelled;
        CancelledBy = actorId;
        CancelledAt = cancelledAt;
        CancellationReason = reason.Trim();
        IncrementVersion();
    }

    public void Archive(DateTimeOffset archivedAt)
    {
        if (Status is not (WorkflowStatus.Completed or WorkflowStatus.Cancelled))
        {
            throw InvalidTransition(WorkflowStatus.Archived);
        }

        Status = WorkflowStatus.Archived;
        ArchivedAt = archivedAt;
        IncrementVersion();
    }

    private void ActivateNextStage(DateTimeOffset activatedAt)
    {
        if (_stages.Any(stage => stage.Status is WorkflowStageStatus.Active or WorkflowStageStatus.Blocked))
        {
            return;
        }

        _stages
            .Where(stage => stage.Status == WorkflowStageStatus.Pending)
            .OrderBy(stage => stage.Order)
            .FirstOrDefault()
            ?.Activate(activatedAt);
    }

    private void RefreshWorkflowStatus(DateTimeOffset? completedAt = null)
    {
        var requiredStagesFinished = _stages
            .Where(stage => !stage.IsOptional)
            .All(stage => stage.Status == WorkflowStageStatus.Completed);

        var optionalStagesFinished = _stages
            .Where(stage => stage.IsOptional)
            .All(stage => stage.Status is WorkflowStageStatus.Completed or WorkflowStageStatus.Skipped);

        if (requiredStagesFinished && optionalStagesFinished)
        {
            Status = WorkflowStatus.Completed;
            CompletedAt = completedAt;
            return;
        }

        var currentStages = CurrentStages;
        Status = currentStages.Count > 0 &&
                 currentStages.All(stage => stage.Status == WorkflowStageStatus.Blocked)
            ? WorkflowStatus.Blocked
            : WorkflowStatus.Active;
    }

    private void EnsureOperational()
    {
        if (Status is not (WorkflowStatus.Active or WorkflowStatus.Blocked))
        {
            throw new WorkflowDomainException(
                "workflow.not_operational",
                "Only an active or blocked workflow can accept this operation.");
        }
    }

    private void EnsureDependenciesComplete(WorkflowTask task)
    {
        var incompleteDependency = task.DependencySourceDefinitionIds
            .Select(FindTaskBySourceDefinition)
            .FirstOrDefault(dependency => dependency.Status != WorkflowTaskStatus.Completed);

        if (incompleteDependency is not null)
        {
            throw new WorkflowDomainException(
                "workflow.task_dependency_incomplete",
                $"Task '{task.Name}' depends on incomplete task '{incompleteDependency.Name}'.");
        }
    }

    private void EnsureTaskStageActive(WorkflowTask task)
    {
        var stage = FindStageContaining(task);
        if (stage.Status is not (WorkflowStageStatus.Active or WorkflowStageStatus.Blocked))
        {
            throw new WorkflowDomainException(
                "workflow.task_stage_not_active",
                $"Task '{task.Name}' belongs to a stage that is not active.");
        }
    }

    private WorkflowTask FindTask(Guid taskId) =>
        _stages.SelectMany(stage => stage.Tasks).SingleOrDefault(task => task.Id == taskId)
        ?? throw new WorkflowDomainException("workflow.task_not_found", "The workflow task was not found.");

    private WorkflowTask FindTaskBySourceDefinition(Guid sourceDefinitionId) =>
        _stages.SelectMany(stage => stage.Tasks)
            .Single(task => task.SourceDefinitionId == sourceDefinitionId);

    private WorkflowStage FindStage(Guid stageId) =>
        _stages.SingleOrDefault(stage => stage.Id == stageId)
        ?? throw new WorkflowDomainException("workflow.stage_not_found", "The workflow stage was not found.");

    private WorkflowStage FindStageContaining(WorkflowTask task) =>
        _stages.Single(stage => stage.Tasks.Contains(task));

    private void IncrementVersion() => Version++;

    private WorkflowDomainException InvalidTransition(WorkflowStatus target) =>
        new(
            "workflow.transition_invalid",
            $"Workflow status cannot transition from {Status} to {target}.");
}
