using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CortexiaAuth.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddHideObjectsWithoutStreet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HideObjectsWithoutStreet",
                table: "DetectionDisplaySettings",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HideObjectsWithoutStreet",
                table: "DetectionDisplaySettings");
        }
    }
}
