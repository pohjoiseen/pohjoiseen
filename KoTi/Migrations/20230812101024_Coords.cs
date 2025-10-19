using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KoTi.Migrations
{
    /// <inheritdoc />
    public partial class Coords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Alias",
                table: "Places",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "Lat",
                table: "Places",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Lng",
                table: "Places",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "Zoom",
                table: "Places",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "Lat",
                table: "Areas",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Lng",
                table: "Areas",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "Zoom",
                table: "Areas",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Alias",
                table: "Places");

            migrationBuilder.DropColumn(
                name: "Lat",
                table: "Places");

            migrationBuilder.DropColumn(
                name: "Lng",
                table: "Places");

            migrationBuilder.DropColumn(
                name: "Zoom",
                table: "Places");

            migrationBuilder.DropColumn(
                name: "Lat",
                table: "Areas");

            migrationBuilder.DropColumn(
                name: "Lng",
                table: "Areas");

            migrationBuilder.DropColumn(
                name: "Zoom",
                table: "Areas");
        }
    }
}
