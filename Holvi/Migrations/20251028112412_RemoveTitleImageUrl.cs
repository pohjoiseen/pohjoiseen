using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Holvi.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTitleImageUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TitleImage",
                table: "Posts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TitleImage",
                table: "Posts",
                type: "TEXT",
                nullable: true);
        }
    }
}
