using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CortexiaAuth.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddManageCortexiaPermission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Permissions_ManageCortexia",
                table: "Roles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Avant ce droit, la page Créer un compte affichait les champs Cortexia uniquement pour
            // le rôle nommé "Admin" (comparaison en dur sur le nom) : on préserve ce comportement.
            migrationBuilder.Sql("""UPDATE "Roles" SET "Permissions_ManageCortexia" = true WHERE "Name" ILIKE 'admin'""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Permissions_ManageCortexia",
                table: "Roles");
        }
    }
}
