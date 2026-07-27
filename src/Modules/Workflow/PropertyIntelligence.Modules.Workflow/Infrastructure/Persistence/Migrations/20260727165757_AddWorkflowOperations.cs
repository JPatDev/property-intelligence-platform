using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyIntelligence.Modules.Workflow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkflowOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "due_at",
                schema: "workflow",
                table: "workflow_task",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "next_action",
                schema: "workflow",
                columns: table => new
                {
                    workflow_id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    task_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: true),
                    due_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    calculated_priority = table.Column<int>(type: "integer", nullable: false),
                    reason_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    calculated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    calculation_version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_next_action", x => x.workflow_id);
                    table.ForeignKey(
                        name: "fk_next_action_workflows_workflow_id",
                        column: x => x.workflow_id,
                        principalSchema: "workflow",
                        principalTable: "workflow_instance",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workflow_escalation",
                schema: "workflow",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workflow_id = table.Column<Guid>(type: "uuid", nullable: false),
                    task_id = table.Column<Guid>(type: "uuid", nullable: true),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    severity = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    deduplication_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    triggered_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_observed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    acknowledged_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    acknowledged_by = table.Column<Guid>(type: "uuid", nullable: true),
                    resolved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    resolution_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workflow_escalation", x => x.id);
                    table.ForeignKey(
                        name: "fk_workflow_escalation_workflows_workflow_id",
                        column: x => x.workflow_id,
                        principalSchema: "workflow",
                        principalTable: "workflow_instance",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_workflow_task_organization_id_status_due_at",
                schema: "workflow",
                table: "workflow_task",
                columns: new[] { "organization_id", "status", "due_at" });

            migrationBuilder.CreateIndex(
                name: "ix_next_action_organization_id_due_at",
                schema: "workflow",
                table: "next_action",
                columns: new[] { "organization_id", "due_at" });

            migrationBuilder.CreateIndex(
                name: "ix_next_action_organization_id_owner_id_calculated_priority",
                schema: "workflow",
                table: "next_action",
                columns: new[] { "organization_id", "owner_id", "calculated_priority" });

            migrationBuilder.CreateIndex(
                name: "ix_workflow_escalation_organization_id_deduplication_key",
                schema: "workflow",
                table: "workflow_escalation",
                columns: new[] { "organization_id", "deduplication_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_workflow_escalation_organization_id_status_severity",
                schema: "workflow",
                table: "workflow_escalation",
                columns: new[] { "organization_id", "status", "severity" });

            migrationBuilder.CreateIndex(
                name: "ix_workflow_escalation_organization_id_task_id",
                schema: "workflow",
                table: "workflow_escalation",
                columns: new[] { "organization_id", "task_id" });

            migrationBuilder.CreateIndex(
                name: "ix_workflow_escalation_workflow_id",
                schema: "workflow",
                table: "workflow_escalation",
                column: "workflow_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "next_action",
                schema: "workflow");

            migrationBuilder.DropTable(
                name: "workflow_escalation",
                schema: "workflow");

            migrationBuilder.DropIndex(
                name: "ix_workflow_task_organization_id_status_due_at",
                schema: "workflow",
                table: "workflow_task");

            migrationBuilder.DropColumn(
                name: "due_at",
                schema: "workflow",
                table: "workflow_task");
        }
    }
}
