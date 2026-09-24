using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace PropertyIntelligence.Modules.Properties.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "properties");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.CreateTable(
                name: "property",
                schema: "properties",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    street = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    state = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    postal_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    address_key = table.Column<string>(type: "character varying(415)", maxLength: 415, nullable: false),
                    county = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    parcel_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    property_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    year_built = table.Column<int>(type: "integer", nullable: true),
                    square_feet = table.Column<int>(type: "integer", nullable: true),
                    roof_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    roof_installation_year = table.Column<int>(type: "integer", nullable: true),
                    owner_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    occupancy_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    location = table.Column<Point>(type: "geometry (point, 4326)", nullable: true),
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
                    table.PrimaryKey("pk_property", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_property_location",
                schema: "properties",
                table: "property",
                column: "location")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "ix_property_organization_id_address_key",
                schema: "properties",
                table: "property",
                columns: new[] { "organization_id", "address_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_property_organization_id_county",
                schema: "properties",
                table: "property",
                columns: new[] { "organization_id", "county" });

            migrationBuilder.CreateIndex(
                name: "ix_property_organization_id_parcel_number",
                schema: "properties",
                table: "property",
                columns: new[] { "organization_id", "parcel_number" },
                unique: true,
                filter: "parcel_number IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "property",
                schema: "properties");
        }
    }
}
