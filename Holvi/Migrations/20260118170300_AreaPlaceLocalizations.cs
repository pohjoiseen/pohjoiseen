using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Holvi.Migrations
{
    /// <inheritdoc />
    public partial class AreaPlaceLocalizations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Alias",
                table: "Areas",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsPrivate",
                table: "Areas",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "AreaLocalizations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AreaId = table.Column<int>(type: "INTEGER", nullable: false),
                    Language = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Alias = table.Column<string>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: false),
                    Links = table.Column<string>(type: "TEXT", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    IsPrivate = table.Column<bool>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AreaLocalizations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AreaLocalizations_Areas_AreaId",
                        column: x => x.AreaId,
                        principalTable: "Areas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlaceLocalizations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlaceId = table.Column<int>(type: "INTEGER", nullable: false),
                    Language = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Alias = table.Column<string>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: false),
                    Links = table.Column<string>(type: "TEXT", nullable: false),
                    Directions = table.Column<string>(type: "TEXT", nullable: false),
                    PublicTransport = table.Column<string>(type: "TEXT", nullable: false),
                    Season = table.Column<string>(type: "TEXT", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    IsPrivate = table.Column<bool>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlaceLocalizations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlaceLocalizations_Places_PlaceId",
                        column: x => x.PlaceId,
                        principalTable: "Places",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Areas_IsPrivate",
                table: "Areas",
                column: "IsPrivate");

            migrationBuilder.CreateIndex(
                name: "IX_AreaLocalizations_AreaId",
                table: "AreaLocalizations",
                column: "AreaId");

            migrationBuilder.CreateIndex(
                name: "IX_PlaceLocalizations_PlaceId",
                table: "PlaceLocalizations",
                column: "PlaceId");
            
            migrationBuilder.Sql(@"
CREATE TRIGGER AreaLocalizations_TimestampUpdate AFTER UPDATE OF Name, Alias, Notes, Links ON AreaLocalizations BEGIN
    UPDATE AreaLocalizations SET UpdatedAt = CURRENT_TIMESTAMP WHERE Id = new.Id;
END");
            migrationBuilder.Sql(@"
CREATE TRIGGER AreaLocalizations_TimestampInsert AFTER INSERT ON AreaLocalizations BEGIN
    UPDATE AreaLocalizations SET UpdatedAt = CURRENT_TIMESTAMP WHERE Id = new.Id;
END");
            migrationBuilder.Sql(@"
CREATE TRIGGER PlaceLocalizations_TimestampUpdate AFTER UPDATE OF Name, Alias, Notes, Links, Directions, PublicTransport, Season ON PlaceLocalizations BEGIN
    UPDATE PlaceLocalizations SET UpdatedAt = CURRENT_TIMESTAMP WHERE Id = new.Id;
END");
            migrationBuilder.Sql(@"
CREATE TRIGGER PlaceLocalizations_TimestampInsert AFTER INSERT ON PlaceLocalizations BEGIN
    UPDATE PlaceLocalizations SET UpdatedAt = CURRENT_TIMESTAMP WHERE Id = new.Id;
END");        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS AreaLocalizations_TimestampUpdate");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS AreaLocalizations_TimestampInsert");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS PlaceLocalizations_TimestampUpdate");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS PlaceLocalizations_TimestampInsert");

            migrationBuilder.DropTable(
                name: "AreaLocalizations");

            migrationBuilder.DropTable(
                name: "PlaceLocalizations");

            migrationBuilder.DropIndex(
                name: "IX_Areas_IsPrivate",
                table: "Areas");

            migrationBuilder.DropColumn(
                name: "Alias",
                table: "Areas");

            migrationBuilder.DropColumn(
                name: "IsPrivate",
                table: "Areas");
        }
    }
}
