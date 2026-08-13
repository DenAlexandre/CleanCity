using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CortexiaAuth.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDetectionDisplaySettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DetectionDisplaySettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PositiveMin = table.Column<double>(type: "double precision", nullable: false),
                    PositiveMax = table.Column<double>(type: "double precision", nullable: false),
                    PositiveColor = table.Column<string>(type: "text", nullable: false),
                    AverageMin = table.Column<double>(type: "double precision", nullable: false),
                    AverageMax = table.Column<double>(type: "double precision", nullable: false),
                    AverageColor = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetectionDisplaySettings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DetectionDisplaySettings");
        }
    }
}
