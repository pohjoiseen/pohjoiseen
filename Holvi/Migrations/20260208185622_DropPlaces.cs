using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Holvi.Migrations
{
    /// <inheritdoc />
    public partial class DropPlaces : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("PRAGMA foreign_keys = OFF;", suppressTransaction: true);

            migrationBuilder.DropForeignKey(
                name: "FK_Pictures_Places_PlaceId",
                table: "Pictures");

            migrationBuilder.DropTable(
                name: "PlaceLocalizations");

            migrationBuilder.DropTable(
                name: "PlaceTag");

            migrationBuilder.DropTable(
                name: "Places");

            migrationBuilder.DropIndex(
                name: "IX_Pictures_PlaceId",
                table: "Pictures");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Places",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ParentId = table.Column<int>(type: "INTEGER", nullable: true),
                    ContentMD = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    Draft = table.Column<bool>(type: "INTEGER", nullable: false),
                    ExploreStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    Icon = table.Column<string>(type: "TEXT", nullable: true),
                    IsLeaf = table.Column<bool>(type: "INTEGER", nullable: false),
                    Lat = table.Column<double>(type: "REAL", nullable: false),
                    Links = table.Column<string>(type: "TEXT", nullable: true),
                    Lng = table.Column<double>(type: "REAL", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    Rating = table.Column<int>(type: "INTEGER", nullable: false),
                    Subtitle = table.Column<string>(type: "TEXT", nullable: true),
                    Title = table.Column<string>(type: "TEXT", nullable: true),
                    TitleImageOffsetY = table.Column<int>(type: "INTEGER", nullable: true),
                    TitlePictureId = table.Column<int>(type: "INTEGER", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Zoom = table.Column<int>(type: "INTEGER", nullable: false),
                    Meta = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Places", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Places_Places_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Places",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PlaceLocalizations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlaceId = table.Column<int>(type: "INTEGER", nullable: false),
                    ContentMD = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Draft = table.Column<bool>(type: "INTEGER", nullable: false),
                    Language = table.Column<string>(type: "TEXT", nullable: false),
                    Links = table.Column<string>(type: "TEXT", nullable: false),
                    Subtitle = table.Column<string>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Meta = table.Column<string>(type: "TEXT", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "PlaceTag",
                columns: table => new
                {
                    PlacesId = table.Column<int>(type: "INTEGER", nullable: false),
                    TagsId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlaceTag", x => new { x.PlacesId, x.TagsId });
                    table.ForeignKey(
                        name: "FK_PlaceTag_Places_PlacesId",
                        column: x => x.PlacesId,
                        principalTable: "Places",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlaceTag_Tags_TagsId",
                        column: x => x.TagsId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Pictures_PlaceId",
                table: "Pictures",
                column: "PlaceId");

            migrationBuilder.CreateIndex(
                name: "IX_PlaceLocalizations_PlaceId",
                table: "PlaceLocalizations",
                column: "PlaceId");

            migrationBuilder.CreateIndex(
                name: "IX_Places_Draft",
                table: "Places",
                column: "Draft");

            migrationBuilder.CreateIndex(
                name: "IX_Places_ExploreStatus",
                table: "Places",
                column: "ExploreStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Places_Icon",
                table: "Places",
                column: "Icon");

            migrationBuilder.CreateIndex(
                name: "IX_Places_Name",
                table: "Places",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Places_ParentId",
                table: "Places",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_Places_Rating",
                table: "Places",
                column: "Rating");

            migrationBuilder.CreateIndex(
                name: "IX_PlaceTag_TagsId",
                table: "PlaceTag",
                column: "TagsId");

            migrationBuilder.AddForeignKey(
                name: "FK_Pictures_Places_PlaceId",
                table: "Pictures",
                column: "PlaceId",
                principalTable: "Places",
                principalColumn: "Id");
        }
    }
}
