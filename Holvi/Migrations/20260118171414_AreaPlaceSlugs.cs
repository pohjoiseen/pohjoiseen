using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Holvi.Migrations
{
    /// <inheritdoc />
    public partial class AreaPlaceSlugs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Places",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Areas",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Places_Slug",
                table: "Places",
                column: "Slug");

            migrationBuilder.CreateIndex(
                name: "IX_Areas_Slug",
                table: "Areas",
                column: "Slug");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Places_Slug",
                table: "Places");

            migrationBuilder.DropIndex(
                name: "IX_Areas_Slug",
                table: "Areas");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Places");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Areas");
        }
    }
}
