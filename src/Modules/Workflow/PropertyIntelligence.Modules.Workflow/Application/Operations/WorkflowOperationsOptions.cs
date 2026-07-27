namespace PropertyIntelligence.Modules.Workflow.Application.Operations;

public sealed class WorkflowOperationsOptions
{
    public const string SectionName = "Workflow:Operations";

    public TimeSpan DueSoonWindow { get; set; } = TimeSpan.FromHours(48);
    public TimeSpan CriticalOverdueAge { get; set; } = TimeSpan.FromDays(2);
    public TimeSpan BlockerEscalationAge { get; set; } = TimeSpan.FromDays(2);
    public TimeSpan ScanInterval { get; set; } = TimeSpan.FromMinutes(5);
    public int ScanBatchSize { get; set; } = 100;
    public int CalculationVersion { get; set; } = 1;
}
