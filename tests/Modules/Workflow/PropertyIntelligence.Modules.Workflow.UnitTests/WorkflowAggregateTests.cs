using PropertyIntelligence.Modules.Workflow.Domain;

namespace PropertyIntelligence.Modules.Workflow.UnitTests;

public sealed class WorkflowAggregateTests
{
    [Fact]
    public void Start_and_complete_last_required_task_completes_workflow()
    {
        var now = new DateTimeOffset(2026, 7, 27, 12, 0, 0, TimeSpan.Zero);
        var actorId = Guid.NewGuid();
        var workflow = WorkflowTestData.CreateWorkflow(now);
        var task = workflow.Stages.Single().Tasks.Single();

        workflow.Start(now);
        workflow.AssignTask(task.Id, actorId);
        workflow.StartTask(task.Id, now.AddMinutes(1));
        var result = workflow.CompleteTask(task.Id, actorId, now.AddMinutes(2), []);

        Assert.True(result.Succeeded);
        Assert.Equal(WorkflowTaskStatus.Completed, task.Status);
        Assert.Equal(WorkflowStageStatus.Completed, workflow.Stages.Single().Status);
        Assert.Equal(WorkflowStatus.Completed, workflow.Status);
        Assert.Equal(5, workflow.Version);
    }

    [Fact]
    public void Required_gate_failure_prevents_task_completion()
    {
        var now = new DateTimeOffset(2026, 7, 27, 12, 0, 0, TimeSpan.Zero);
        var gateId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var workflow = WorkflowTestData.CreateWorkflow(
            now,
            gates:
            [
                new CompletionGateDefinition(
                    gateId,
                    "DocumentExists",
                    CompletionGateScope.Task,
                    CompletionGateSeverity.Required,
                    new Dictionary<string, string> { ["documentType"] = "Policy" },
                    "POLICY_REQUIRED",
                    "A verified policy is required.",
                    1),
            ]);
        var task = workflow.Stages.Single().Tasks.Single();
        workflow.Start(now);
        workflow.AssignTask(task.Id, actorId);

        var result = workflow.CompleteTask(
            task.Id,
            actorId,
            now.AddMinutes(1),
            [new GateEvaluationResult(gateId, GateEvaluationOutcome.Failed, now)]);

        Assert.False(result.Succeeded);
        var failure = Assert.Single(result.Failures);
        Assert.Equal("POLICY_REQUIRED", failure.Code);
        Assert.Equal(WorkflowTaskStatus.Assigned, task.Status);
        Assert.Equal(WorkflowStatus.Active, workflow.Status);
    }

    [Fact]
    public void Blocking_only_required_task_blocks_workflow_until_resolved()
    {
        var now = new DateTimeOffset(2026, 7, 27, 12, 0, 0, TimeSpan.Zero);
        var actorId = Guid.NewGuid();
        var workflow = WorkflowTestData.CreateWorkflow(now);
        var task = workflow.Stages.Single().Tasks.Single();
        workflow.Start(now);
        workflow.AssignTask(task.Id, actorId);

        var blockerId = workflow.AddTaskBlocker(
            task.Id,
            "WAITING_FOR_CARRIER",
            "Waiting for carrier response.",
            actorId,
            now);

        Assert.Equal(WorkflowTaskStatus.Blocked, task.Status);
        Assert.Equal(WorkflowStageStatus.Blocked, workflow.Stages.Single().Status);
        Assert.Equal(WorkflowStatus.Blocked, workflow.Status);

        workflow.ResolveTaskBlocker(
            task.Id,
            blockerId,
            actorId,
            "Carrier responded.",
            now.AddDays(1));

        Assert.Equal(WorkflowTaskStatus.Assigned, task.Status);
        Assert.Equal(WorkflowStatus.Active, workflow.Status);
    }

    [Fact]
    public void Snapshot_with_dependency_cycle_is_rejected()
    {
        var firstTaskId = Guid.NewGuid();
        var secondTaskId = Guid.NewGuid();
        var snapshot = new WorkflowSnapshot(
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            [
                new StageSnapshot(
                    Guid.NewGuid(),
                    "Investigation",
                    1,
                    false,
                    [
                        new TaskSnapshot(firstTaskId, "First", 1, 1, true, new HashSet<Guid> { secondTaskId }, []),
                        new TaskSnapshot(secondTaskId, "Second", 2, 1, true, new HashSet<Guid> { firstTaskId }, []),
                    ]),
            ]);

        var exception = Assert.Throws<WorkflowDomainException>(() => snapshot.Validate());

        Assert.Equal("workflow.task_dependency_cycle", exception.Code);
    }
}

internal static class WorkflowTestData
{
    public static WorkflowInstance CreateWorkflow(
        DateTimeOffset now,
        IReadOnlyList<CompletionGateDefinition>? gates = null,
        DateTimeOffset? dueAt = null)
    {
        var snapshot = new WorkflowSnapshot(
            Guid.NewGuid(),
            Guid.NewGuid(),
            1,
            [
                new StageSnapshot(
                    Guid.NewGuid(),
                    "Investigation",
                    1,
                    false,
                    [
                        new TaskSnapshot(
                            Guid.NewGuid(),
                            "Inspect property",
                            1,
                            10,
                            true,
                            new HashSet<Guid>(),
                            gates ?? [],
                            dueAt),
                    ]),
            ]);

        return WorkflowInstance.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            WorkflowType.Primary,
            snapshot,
            now);
    }
}
