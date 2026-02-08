using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Holvi.Migrations
{
    /// <inheritdoc />
    public partial class Books : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BookId",
                table: "Posts",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Order",
                table: "Posts",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Books",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Draft = table.Column<bool>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    ContentMD = table.Column<string>(type: "TEXT", nullable: false),
                    Language = table.Column<string>(type: "TEXT", maxLength: 2, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Books", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Posts_BookId",
                table: "Posts",
                column: "BookId");

            migrationBuilder.CreateIndex(
                name: "IX_Books_Name",
                table: "Books",
                column: "Name");

            migrationBuilder.AddForeignKey(
                name: "FK_Posts_Books_BookId",
                table: "Posts",
                column: "BookId",
                principalTable: "Books",
                principalColumn: "Id");
            
            migrationBuilder.Sql(@"
CREATE TRIGGER Books_TimestampUpdate AFTER UPDATE OF Name, Title, ContentMD, Draft ON Books BEGIN
    UPDATE Books SET UpdatedAt = CURRENT_TIMESTAMP WHERE Id = new.Id;
END");
            migrationBuilder.Sql(@"
CREATE TRIGGER Books_TimestampInsert AFTER INSERT ON Books BEGIN
    UPDATE Books SET UpdatedAt = CURRENT_TIMESTAMP WHERE Id = new.Id;
END");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS Books_TimestampUpdate");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS Books_TimestampInsert");

            migrationBuilder.DropForeignKey(
                name: "FK_Posts_Books_BookId",
                table: "Posts");

            migrationBuilder.DropTable(
                name: "Books");

            migrationBuilder.DropIndex(
                name: "IX_Posts_BookId",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "BookId",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "Order",
                table: "Posts");
        }
    }
}
