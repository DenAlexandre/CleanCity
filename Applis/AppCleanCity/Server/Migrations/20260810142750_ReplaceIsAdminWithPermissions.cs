using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CortexiaAuth.Api.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceIsAdminWithPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsAdmin",
                table: "AppUsers",
                newName: "Permissions_ViewPointsInteret");

            migrationBuilder.AddColumn<bool>(
                name: "Permissions_ManageAccounts",
                table: "AppUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Permissions_ViewAlertes",
                table: "AppUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Permissions_ViewExport",
                table: "AppUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Permissions_ViewItineraires",
                table: "AppUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Permissions_ViewListeMesures",
                table: "AppUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Permissions_ViewMesures",
                table: "AppUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Permissions_ViewParametres",
                table: "AppUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Permissions_ManageAccounts",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "Permissions_ViewAlertes",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "Permissions_ViewExport",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "Permissions_ViewItineraires",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "Permissions_ViewListeMesures",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "Permissions_ViewMesures",
                table: "AppUsers");

            migrationBuilder.DropColumn(
                name: "Permissions_ViewParametres",
                table: "AppUsers");

            migrationBuilder.RenameColumn(
                name: "Permissions_ViewPointsInteret",
                table: "AppUsers",
                newName: "IsAdmin");
        }
    }
}
