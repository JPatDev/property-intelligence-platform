using Microsoft.Extensions.Options;
using PropertyIntelligence.Modules.Workflow.Application.Operations;

namespace PropertyIntelligence.Modules.Workflow.UnitTests;

public sealed class NextActionCalculatorTests
{
    [Fact]
    public void Critically_overdue_blocked_task_prioritizes_blocker_resolution()
    {
        var now = new DateTimeOffset(2026, 7, 27, 12, 0, 0, TimeSpan.Zero);
        var actorId = Guid.NewGuid();
        var workflow = WorkflowTestData.CreateWorkflow(now, dueAt: now.AddDays(-3));
        var task = workflow.Stages.Single().Tasks.Single();
        workflow.Start(now.AddDays(-4));
        workflow.AssignTask(task.Id, actorId);
        workflow.AddTaskBlocker(
            task.Id,
            "MISSING_ACCESS",
            "Property access is unavailable.",
            actorId,
            now.AddDays(-3));
        var calculator = new NextActionCalculator(
            Options.Create(new WorkflowOperationsOptions()));

        var result = calculator.Calculate(workflow, now);

        Assert.NotNull(result);
        Assert.Equal(task.Id, result.TaskId);
        Assert.Equal("CRITICALLY_OVERDUE_TASK_BLOCKED", result.ReasonCode);
        Assert.StartsWith("Resolve blocker", result.Action);
    }
}
