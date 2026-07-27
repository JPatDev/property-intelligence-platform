using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PropertyIntelligence.Modules.Claims.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialClaims : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "claims");

            migrationBuilder.CreateTable(
                name: "claim",
                schema: "claims",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    claim_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    policy_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    date_of_loss = table.Column<DateOnly>(type: "date", nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modified_by = table.Column<Guid>(type: "uuid", nullable: true),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_claim", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_claim_organization_id_claim_number",
                schema: "claims",
                table: "claim",
                columns: new[] { "organization_id", "claim_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_claim_organization_id_property_id",
                schema: "claims",
                table: "claim",
                columns: new[] { "organization_id", "property_id" });

            migrationBuilder.CreateIndex(
                name: "ix_claim_organization_id_status",
                schema: "claims",
                table: "claim",
                columns: new[] { "organization_id", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "claim",
                schema: "claims");
        }
    }
}
