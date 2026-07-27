using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyIntelligence.Modules.Workflow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "workflow");

            migrationBuilder.CreateTable(
                name: "workflow_instance",
                schema: "workflow",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    claim_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    source_playbook_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_playbook_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    snapshot_schema_version = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cancelled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cancelled_by = table.Column<Guid>(type: "uuid", nullable: true),
                    cancellation_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    archived_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workflow_instance", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "workflow_stage",
                schema: "workflow",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    stage_order = table.Column<int>(type: "integer", nullable: false),
                    is_optional = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    activated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    skipped_by = table.Column<Guid>(type: "uuid", nullable: true),
                    skipped_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    skip_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    workflow_instance_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workflow_stage", x => x.id);
                    table.ForeignKey(
                        name: "fk_workflow_stage_workflow_instance_workflow_instance_id",
                        column: x => x.workflow_instance_id,
                        principalSchema: "workflow",
                        principalTable: "workflow_instance",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workflow_task",
                schema: "workflow",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    task_order = table.Column<int>(type: "integer", nullable: false),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    is_required = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    assigned_to = table.Column<Guid>(type: "uuid", nullable: true),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cancelled_by = table.Column<Guid>(type: "uuid", nullable: true),
                    cancelled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cancellation_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    workflow_stage_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workflow_task", x => x.id);
                    table.ForeignKey(
                        name: "fk_workflow_task_workflow_stage_workflow_stage_id",
                        column: x => x.workflow_stage_id,
                        principalSchema: "workflow",
                        principalTable: "workflow_stage",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "completion_gate",
                schema: "workflow",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    gate_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    scope = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    severity = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    parameters = table.Column<string>(type: "jsonb", nullable: false),
                    failure_code = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    failure_message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    evaluation_version = table.Column<int>(type: "integer", nullable: false),
                    workflow_task_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_completion_gate", x => x.id);
                    table.ForeignKey(
                        name: "fk_completion_gate_workflow_task_workflow_task_id",
                        column: x => x.workflow_task_id,
                        principalSchema: "workflow",
                        principalTable: "workflow_task",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workflow_blocker",
                schema: "workflow",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    resolved_by = table.Column<Guid>(type: "uuid", nullable: true),
                    resolved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    resolution_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    workflow_task_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workflow_blocker", x => x.id);
                    table.ForeignKey(
                        name: "fk_workflow_blocker_workflow_task_workflow_task_id",
                        column: x => x.workflow_task_id,
                        principalSchema: "workflow",
                        principalTable: "workflow_task",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "workflow_task_dependency",
                schema: "workflow",
                columns: table => new
                {
                    task_id = table.Column<Guid>(type: "uuid", nullable: false),
                    dependency_source_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_workflow_task_dependency", x => new { x.task_id, x.dependency_source_definition_id });
                    table.ForeignKey(
                        name: "fk_workflow_task_dependency_workflow_task_task_id",
                        column: x => x.task_id,
                        principalSchema: "workflow",
                        principalTable: "workflow_task",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_completion_gate_organization_id_gate_type",
                schema: "workflow",
                table: "completion_gate",
                columns: new[] { "organization_id", "gate_type" });

            migrationBuilder.CreateIndex(
                name: "ix_completion_gate_workflow_task_id_id",
                schema: "workflow",
                table: "completion_gate",
                columns: new[] { "workflow_task_id", "id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_workflow_blocker_organization_id_workflow_task_id_resolved_",
                schema: "workflow",
                table: "workflow_blocker",
                columns: new[] { "organization_id", "workflow_task_id", "resolved_at" });

            migrationBuilder.CreateIndex(
                name: "ix_workflow_blocker_workflow_task_id",
                schema: "workflow",
                table: "workflow_blocker",
                column: "workflow_task_id");

            migrationBuilder.CreateIndex(
                name: "ix_workflow_instance_organization_id_claim_id",
                schema: "workflow",
                table: "workflow_instance",
                columns: new[] { "organization_id", "claim_id" });

            migrationBuilder.CreateIndex(
                name: "ix_workflow_instance_organization_id_source_playbook_version_id",
                schema: "workflow",
                table: "workflow_instance",
                columns: new[] { "organization_id", "source_playbook_version_id" });

            migrationBuilder.CreateIndex(
                name: "ix_workflow_instance_organization_id_status",
                schema: "workflow",
                table: "workflow_instance",
                columns: new[] { "organization_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_workflow_stage_organization_id_status",
                schema: "workflow",
                table: "workflow_stage",
                columns: new[] { "organization_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_workflow_stage_workflow_instance_id_source_definition_id",
                schema: "workflow",
                table: "workflow_stage",
                columns: new[] { "workflow_instance_id", "source_definition_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_workflow_stage_workflow_instance_id_stage_order",
                schema: "workflow",
                table: "workflow_stage",
                columns: new[] { "workflow_instance_id", "stage_order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_workflow_task_organization_id_status_assigned_to",
                schema: "workflow",
                table: "workflow_task",
                columns: new[] { "organization_id", "status", "assigned_to" });

            migrationBuilder.CreateIndex(
                name: "ix_workflow_task_workflow_stage_id_source_definition_id",
                schema: "workflow",
                table: "workflow_task",
                columns: new[] { "workflow_stage_id", "source_definition_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_workflow_task_workflow_stage_id_task_order",
                schema: "workflow",
                table: "workflow_task",
                columns: new[] { "workflow_stage_id", "task_order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_workflow_task_dependency_organization_id_dependency_source_",
                schema: "workflow",
                table: "workflow_task_dependency",
                columns: new[] { "organization_id", "dependency_source_definition_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "completion_gate",
                schema: "workflow");

            migrationBuilder.DropTable(
                name: "workflow_blocker",
                schema: "workflow");

            migrationBuilder.DropTable(
                name: "workflow_task_dependency",
                schema: "workflow");

            migrationBuilder.DropTable(
                name: "workflow_task",
                schema: "workflow");

            migrationBuilder.DropTable(
                name: "workflow_stage",
                schema: "workflow");

            migrationBuilder.DropTable(
                name: "workflow_instance",
                schema: "workflow");
        }
    }
}
