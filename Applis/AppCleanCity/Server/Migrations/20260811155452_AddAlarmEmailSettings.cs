using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CortexiaAuth.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAlarmEmailSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "SendEmail",
                table: "AlarmThresholds",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "AlarmEmailRecipients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Email = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlarmEmailRecipients", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlarmEmailRecipients_Email",
                table: "AlarmEmailRecipients",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlarmEmailRecipients");

            migrationBuilder.DropColumn(
                name: "SendEmail",
                table: "AlarmThresholds");
        }
    }
}
