using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyIntelligence.Modules.Workflow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkflowAuditAndOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "outbox_message",
                schema: "workflow",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    aggregate_type = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    aggregate_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    available_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    processed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    last_error = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    correlation_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    causation_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbox_message", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "workflow_audit",
                schema: "workflow",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workflow_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entity_type = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    action = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    previous_state = table.Column<string>(type: "jsonb", nullable: true),
                    new_state = table.Column<string>(type: "jsonb", nullable: true),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    correlation_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    causation_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    system_generated = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workflow_audit", x => x.id);
                    table.ForeignKey(
                        name: "fk_workflow_audit_workflows_workflow_id",
                        column: x => x.workflow_id,
                        principalSchema: "workflow",
                        principalTable: "workflow_instance",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_outbox_message_organization_id_aggregate_id_occurred_at",
                schema: "workflow",
                table: "outbox_message",
                columns: new[] { "organization_id", "aggregate_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_outbox_message_processed_at_available_at_occurred_at",
                schema: "workflow",
                table: "outbox_message",
                columns: new[] { "processed_at", "available_at", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_workflow_audit_organization_id_correlation_id",
                schema: "workflow",
                table: "workflow_audit",
                columns: new[] { "organization_id", "correlation_id" });

            migrationBuilder.CreateIndex(
                name: "ix_workflow_audit_organization_id_workflow_id_occurred_at",
                schema: "workflow",
                table: "workflow_audit",
                columns: new[] { "organization_id", "workflow_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_workflow_audit_workflow_id",
                schema: "workflow",
                table: "workflow_audit",
                column: "workflow_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "outbox_message",
                schema: "workflow");

            migrationBuilder.DropTable(
                name: "workflow_audit",
                schema: "workflow");
        }
    }
}
