using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CortexiaAuth.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddImportCheckpoints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ImportCheckpoints",
                columns: table => new
                {
                    Dataset = table.Column<string>(type: "text", nullable: false),
                    LastImportedUntilUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportCheckpoints", x => x.Dataset);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImportCheckpoints");
        }
    }
}
