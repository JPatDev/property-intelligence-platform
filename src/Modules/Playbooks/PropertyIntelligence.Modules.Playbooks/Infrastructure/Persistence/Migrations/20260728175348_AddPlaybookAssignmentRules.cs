using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyIntelligence.Modules.Playbooks.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlaybookAssignmentRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "assignment_rules_json",
                schema: "playbooks",
                table: "playbook_version",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'[]'::jsonb");

            migrationBuilder.CreateTable(
                name: "playbook_assignment_evaluation",
                schema: "playbooks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    claim_id = table.Column<Guid>(type: "uuid", nullable: false),
                    evaluated_by = table.Column<Guid>(type: "uuid", nullable: false),
                    evaluated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    results_json = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_playbook_assignment_evaluation", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_playbook_assignment_evaluation_organization_id_claim_id_eva",
                schema: "playbooks",
                table: "playbook_assignment_evaluation",
                columns: new[] { "organization_id", "claim_id", "evaluated_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "playbook_assignment_evaluation",
                schema: "playbooks");

            migrationBuilder.DropColumn(
                name: "assignment_rules_json",
                schema: "playbooks",
                table: "playbook_version");
        }
    }
}
