using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CortexiaAuth.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Permissions_ManageAccounts = table.Column<bool>(type: "boolean", nullable: false),
                    Permissions_ViewMesures = table.Column<bool>(type: "boolean", nullable: false),
                    Permissions_ViewListeMesures = table.Column<bool>(type: "boolean", nullable: false),
                    Permissions_ViewItineraires = table.Column<bool>(type: "boolean", nullable: false),
                    Permissions_ViewPointsInteret = table.Column<bool>(type: "boolean", nullable: false),
                    Permissions_ViewAlertes = table.Column<bool>(type: "boolean", nullable: false),
                    Permissions_ViewParametres = table.Column<bool>(type: "boolean", nullable: false),
                    Permissions_ViewExport = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Name",
                table: "Roles",
                column: "Name",
                unique: true);

            // Rôles par défaut : Admin reprend les droits de tout compte qui avait déjà ManageAccounts,
            // User reprend les autres droits de consultation pour les comptes qui ne l'avaient pas.
            migrationBuilder.Sql(
                """
                INSERT INTO "Roles" ("Name", "Permissions_ManageAccounts", "Permissions_ViewMesures", "Permissions_ViewListeMesures", "Permissions_ViewItineraires", "Permissions_ViewPointsInteret", "Permissions_ViewAlertes", "Permissions_ViewParametres", "Permissions_ViewExport")
                VALUES ('Admin', true, true, true, true, true, true, true, true);
                """);
            migrationBuilder.Sql(
                """
                INSERT INTO "Roles" ("Name", "Permissions_ManageAccounts", "Permissions_ViewMesures", "Permissions_ViewListeMesures", "Permissions_ViewItineraires", "Permissions_ViewPointsInteret", "Permissions_ViewAlertes", "Permissions_ViewParametres", "Permissions_ViewExport")
                VALUES ('User', false, true, true, true, true, true, true, true);
                """);

            migrationBuilder.AddColumn<int>(
                name: "RoleId",
                table: "AppUsers",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "AppUsers"
                SET "RoleId" = (SELECT "Id" FROM "Roles" WHERE "Name" = CASE WHEN "AppUsers"."Permissions_ManageAccounts" THEN 'Admin' ELSE 'User' END);
                """);

            migrationBuilder.AlterColumn<int>(
                name: "RoleId",
                table: "AppUsers",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

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

            migrationBuilder.DropColumn(
                name: "Permissions_ViewPointsInteret",
                table: "AppUsers");

            migrationBuilder.CreateIndex(
                name: "IX_AppUsers_RoleId",
                table: "AppUsers",
                column: "RoleId");

            migrationBuilder.AddForeignKey(
                name: "FK_AppUsers_Roles_RoleId",
                table: "AppUsers",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppUsers_Roles_RoleId",
                table: "AppUsers");

            migrationBuilder.DropIndex(
                name: "IX_AppUsers_RoleId",
                table: "AppUsers");

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

            migrationBuilder.AddColumn<bool>(
                name: "Permissions_ViewPointsInteret",
                table: "AppUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(
                """
                UPDATE "AppUsers" u
                SET "Permissions_ManageAccounts" = r."Permissions_ManageAccounts",
                    "Permissions_ViewMesures" = r."Permissions_ViewMesures",
                    "Permissions_ViewListeMesures" = r."Permissions_ViewListeMesures",
                    "Permissions_ViewItineraires" = r."Permissions_ViewItineraires",
                    "Permissions_ViewPointsInteret" = r."Permissions_ViewPointsInteret",
                    "Permissions_ViewAlertes" = r."Permissions_ViewAlertes",
                    "Permissions_ViewParametres" = r."Permissions_ViewParametres",
                    "Permissions_ViewExport" = r."Permissions_ViewExport"
                FROM "Roles" r
                WHERE u."RoleId" = r."Id";
                """);

            migrationBuilder.DropColumn(
                name: "RoleId",
                table: "AppUsers");

            migrationBuilder.DropTable(
                name: "Roles");
        }
    }
}
