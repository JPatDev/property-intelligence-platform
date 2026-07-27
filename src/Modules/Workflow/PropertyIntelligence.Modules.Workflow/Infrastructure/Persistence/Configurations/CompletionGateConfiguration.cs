using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PropertyIntelligence.Modules.Workflow.Domain;

namespace PropertyIntelligence.Modules.Workflow.Infrastructure.Persistence.Configurations;

internal sealed class CompletionGateConfiguration : IEntityTypeConfiguration<CompletionGateDefinition>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public void Configure(EntityTypeBuilder<CompletionGateDefinition> builder)
    {
        builder.ToTable("completion_gate");
        builder.HasKey(gate => gate.Id);

        builder.Property(gate => gate.Id).ValueGeneratedNever();
        builder.Property(gate => gate.SourceDefinitionId).IsRequired();
        builder.Property(gate => gate.OrganizationId).IsRequired();
        builder.Property(gate => gate.GateType).HasMaxLength(100).IsRequired();
        builder.Property(gate => gate.Scope).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(gate => gate.Severity).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(gate => gate.FailureCode).HasMaxLength(150).IsRequired();
        builder.Property(gate => gate.FailureMessage).HasMaxLength(1000).IsRequired();
        builder.Property(gate => gate.EvaluationVersion).IsRequired();

        var converter = new ValueConverter<IReadOnlyDictionary<string, string>, string>(
            parameters => JsonSerializer.Serialize(parameters, JsonOptions),
            json => JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOptions)
                ?? new Dictionary<string, string>());

        var comparer = new ValueComparer<IReadOnlyDictionary<string, string>>(
            (left, right) => Serialize(left) == Serialize(right),
            parameters => Serialize(parameters).GetHashCode(StringComparison.Ordinal),
            parameters => new Dictionary<string, string>(parameters));

        builder.Property(gate => gate.Parameters)
            .HasConversion(converter, comparer)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.HasIndex("WorkflowTaskId", nameof(CompletionGateDefinition.Id)).IsUnique();
        builder.HasIndex("WorkflowTaskId", nameof(CompletionGateDefinition.SourceDefinitionId)).IsUnique();
        builder.HasIndex(gate => new { gate.OrganizationId, gate.GateType });
    }

    private static string Serialize(IReadOnlyDictionary<string, string>? parameters) =>
        JsonSerializer.Serialize(parameters, JsonOptions);
}
