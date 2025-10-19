using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KoTi.Migrations
{
    /// <inheritdoc />
    public partial class IndicesAndRating : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Rating",
                table: "Places",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Rating",
                table: "Pictures",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Places_Category",
                table: "Places",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Places_ExploreStatus",
                table: "Places",
                column: "ExploreStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Places_IsPrivate",
                table: "Places",
                column: "IsPrivate");

            migrationBuilder.CreateIndex(
                name: "IX_Places_Rating",
                table: "Places",
                column: "Rating");

            migrationBuilder.CreateIndex(
                name: "IX_Pictures_Hash",
                table: "Pictures",
                column: "Hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pictures_IsPrivate",
                table: "Pictures",
                column: "IsPrivate");

            migrationBuilder.CreateIndex(
                name: "IX_Pictures_PhotographedAt",
                table: "Pictures",
                column: "PhotographedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Pictures_Rating",
                table: "Pictures",
                column: "Rating");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Places_Category",
                table: "Places");

            migrationBuilder.DropIndex(
                name: "IX_Places_ExploreStatus",
                table: "Places");

            migrationBuilder.DropIndex(
                name: "IX_Places_IsPrivate",
                table: "Places");

            migrationBuilder.DropIndex(
                name: "IX_Places_Rating",
                table: "Places");

            migrationBuilder.DropIndex(
                name: "IX_Pictures_Hash",
                table: "Pictures");

            migrationBuilder.DropIndex(
                name: "IX_Pictures_IsPrivate",
                table: "Pictures");

            migrationBuilder.DropIndex(
                name: "IX_Pictures_PhotographedAt",
                table: "Pictures");

            migrationBuilder.DropIndex(
                name: "IX_Pictures_Rating",
                table: "Pictures");

            migrationBuilder.DropColumn(
                name: "Rating",
                table: "Places");

            migrationBuilder.DropColumn(
                name: "Rating",
                table: "Pictures");
        }
    }
}
