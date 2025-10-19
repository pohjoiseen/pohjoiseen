using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KoTi.Migrations
{
    /// <inheritdoc />
    public partial class MorePlaceFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Places",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Directions",
                table: "Places",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PublicTransport",
                table: "Places",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Season",
                table: "Places",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData("Places", "Category", "", "Category", "star");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "Places");

            migrationBuilder.DropColumn(
                name: "Directions",
                table: "Places");

            migrationBuilder.DropColumn(
                name: "PublicTransport",
                table: "Places");

            migrationBuilder.DropColumn(
                name: "Season",
                table: "Places");
        }
    }
}
