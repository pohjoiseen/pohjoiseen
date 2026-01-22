using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Holvi.Migrations
{
    /// <inheritdoc />
    public partial class PlaceLocalizationsMetaNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Meta",
                table: "PlaceLocalizations",
                type: "TEXT",
                nullable: false,
                defaultValue: "{}",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Meta",
                table: "PlaceLocalizations",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");
        }
    }
}
