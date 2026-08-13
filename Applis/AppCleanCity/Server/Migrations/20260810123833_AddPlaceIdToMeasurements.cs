using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CortexiaAuth.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPlaceIdToMeasurements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "EdgeV",
                table: "EdgeSnapshots",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<long>(
                name: "EdgeU",
                table: "EdgeSnapshots",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<short>(
                name: "EdgeKey",
                table: "EdgeSnapshots",
                type: "smallint",
                nullable: true,
                oldClrType: typeof(short),
                oldType: "smallint");

            migrationBuilder.AddColumn<string>(
                name: "PlaceId",
                table: "EdgeSnapshots",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "EdgeV",
                table: "EdgeCciMeasurements",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<long>(
                name: "EdgeU",
                table: "EdgeCciMeasurements",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<short>(
                name: "EdgeKey",
                table: "EdgeCciMeasurements",
                type: "smallint",
                nullable: true,
                oldClrType: typeof(short),
                oldType: "smallint");

            migrationBuilder.AddColumn<string>(
                name: "PlaceId",
                table: "EdgeCciMeasurements",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EdgeSnapshots_PlaceId_MeasuredAt",
                table: "EdgeSnapshots",
                columns: new[] { "PlaceId", "MeasuredAt" },
                filter: "\"PlaceId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EdgeCciMeasurements_PlaceId_MeasuredAt",
                table: "EdgeCciMeasurements",
                columns: new[] { "PlaceId", "MeasuredAt" },
                filter: "\"PlaceId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EdgeSnapshots_PlaceId_MeasuredAt",
                table: "EdgeSnapshots");

            migrationBuilder.DropIndex(
                name: "IX_EdgeCciMeasurements_PlaceId_MeasuredAt",
                table: "EdgeCciMeasurements");

            migrationBuilder.DropColumn(
                name: "PlaceId",
                table: "EdgeSnapshots");

            migrationBuilder.DropColumn(
                name: "PlaceId",
                table: "EdgeCciMeasurements");

            migrationBuilder.AlterColumn<long>(
                name: "EdgeV",
                table: "EdgeSnapshots",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "EdgeU",
                table: "EdgeSnapshots",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<short>(
                name: "EdgeKey",
                table: "EdgeSnapshots",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0,
                oldClrType: typeof(short),
                oldType: "smallint",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "EdgeV",
                table: "EdgeCciMeasurements",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "EdgeU",
                table: "EdgeCciMeasurements",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<short>(
                name: "EdgeKey",
                table: "EdgeCciMeasurements",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0,
                oldClrType: typeof(short),
                oldType: "smallint",
                oldNullable: true);
        }
    }
}
