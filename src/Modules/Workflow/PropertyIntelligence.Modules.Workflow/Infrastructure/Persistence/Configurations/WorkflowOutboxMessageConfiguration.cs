using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyIntelligence.Modules.Workflow.Domain;

namespace PropertyIntelligence.Modules.Workflow.Infrastructure.Persistence.Configurations;

internal sealed class WorkflowOutboxMessageConfiguration : IEntityTypeConfiguration<WorkflowOutboxMessage>
{
    public void Configure(EntityTypeBuilder<WorkflowOutboxMessage> builder)
    {
        builder.ToTable("outbox_message");
        builder.HasKey(message => message.Id);

        builder.Property(message => message.Id).ValueGeneratedNever();
        builder.Property(message => message.OrganizationId).IsRequired();
        builder.Property(message => message.AggregateType).HasMaxLength(150).IsRequired();
        builder.Property(message => message.AggregateId).IsRequired();
        builder.Property(message => message.EventType).HasMaxLength(300).IsRequired();
        builder.Property(message => message.Payload).HasColumnType("jsonb").IsRequired();
        builder.Property(message => message.OccurredAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(message => message.AvailableAt).HasColumnType("timestamp with time zone").IsRequired();
        builder.Property(message => message.ProcessedAt).HasColumnType("timestamp with time zone");
        builder.Property(message => message.AttemptCount).IsRequired();
        builder.Property(message => message.LastError).HasMaxLength(4000);
        builder.Property(message => message.CorrelationId).HasMaxLength(100).IsRequired();
        builder.Property(message => message.CausationId).HasMaxLength(200).IsRequired();

        builder.HasIndex(message => new
        {
            message.ProcessedAt,
            message.AvailableAt,
            message.OccurredAt,
        });
        builder.HasIndex(message => new
        {
            message.OrganizationId,
            message.AggregateId,
            message.OccurredAt,
        });
    }
}
