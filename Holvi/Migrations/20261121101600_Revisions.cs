using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Holvi.Migrations
{
    /// <inheritdoc />
    public partial class Revisions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Revisions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TargetType = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    TargetId = table.Column<int>(type: "INTEGER", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    ContentGz = table.Column<byte[]>(type: "BLOB", nullable: false),
                    ContentLength = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SavedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Keep = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Revisions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Revisions_TargetType_TargetId",
                table: "Revisions",
                columns: new[] { "TargetType", "TargetId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Revisions");
        }
    }
}
