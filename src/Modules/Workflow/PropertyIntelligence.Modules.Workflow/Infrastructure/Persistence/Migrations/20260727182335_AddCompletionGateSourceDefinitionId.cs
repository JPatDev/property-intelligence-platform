using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyIntelligence.Modules.Workflow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCompletionGateSourceDefinitionId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "source_definition_id",
                schema: "workflow",
                table: "completion_gate",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE workflow.completion_gate
                SET source_definition_id = id
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "source_definition_id",
                schema: "workflow",
                table: "completion_gate",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_completion_gate_workflow_task_id_source_definition_id",
                schema: "workflow",
                table: "completion_gate",
                columns: new[] { "workflow_task_id", "source_definition_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_completion_gate_workflow_task_id_source_definition_id",
                schema: "workflow",
                table: "completion_gate");

            migrationBuilder.DropColumn(
                name: "source_definition_id",
                schema: "workflow",
                table: "completion_gate");
        }
    }
}
