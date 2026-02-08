using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Holvi.Migrations
{
    /// <inheritdoc />
    public partial class BooksTitlePicture : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TitlePictureId",
                table: "Books",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Books_TitlePictureId",
                table: "Books",
                column: "TitlePictureId");

            migrationBuilder.AddForeignKey(
                name: "FK_Books_Pictures_TitlePictureId",
                table: "Books",
                column: "TitlePictureId",
                principalTable: "Pictures",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Books_Pictures_TitlePictureId",
                table: "Books");

            migrationBuilder.DropIndex(
                name: "IX_Books_TitlePictureId",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "TitlePictureId",
                table: "Books");
        }
    }
}
