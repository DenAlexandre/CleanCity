using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CortexiaAuth.Api.Migrations
{
    /// <inheritdoc />
    public partial class RenameViewExportToViewSysteme : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Permissions_ViewExport",
                table: "Roles",
                newName: "Permissions_ViewSysteme");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Permissions_ViewSysteme",
                table: "Roles",
                newName: "Permissions_ViewExport");
        }
    }
}
