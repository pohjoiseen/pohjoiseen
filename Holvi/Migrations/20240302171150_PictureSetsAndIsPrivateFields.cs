using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Holvi.Migrations
{
    /// <inheritdoc />
    public partial class PictureSetsAndIsPrivateFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPrivate",
                table: "Places",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPrivate",
                table: "Pictures",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "SetId",
                table: "Pictures",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PictureSets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    IsPrivate = table.Column<bool>(type: "INTEGER", nullable: false),
                    ParentId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PictureSets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PictureSets_PictureSets_ParentId",
                        column: x => x.ParentId,
                        principalTable: "PictureSets",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Pictures_SetId",
                table: "Pictures",
                column: "SetId");

            migrationBuilder.CreateIndex(
                name: "IX_PictureSets_ParentId",
                table: "PictureSets",
                column: "ParentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Pictures_PictureSets_SetId",
                table: "Pictures",
                column: "SetId",
                principalTable: "PictureSets",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Pictures_PictureSets_SetId",
                table: "Pictures");

            migrationBuilder.DropTable(
                name: "PictureSets");

            migrationBuilder.DropIndex(
                name: "IX_Pictures_SetId",
                table: "Pictures");

            migrationBuilder.DropColumn(
                name: "IsPrivate",
                table: "Places");

            migrationBuilder.DropColumn(
                name: "IsPrivate",
                table: "Pictures");

            migrationBuilder.DropColumn(
                name: "SetId",
                table: "Pictures");
        }
    }
}
