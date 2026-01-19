using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Holvi.Migrations
{
    /// <inheritdoc />
    public partial class PlaceOverhaul : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("PRAGMA foreign_keys = OFF;", suppressTransaction: true);

            migrationBuilder.CreateTable(
                name: "PlacesNew",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ParentId = table.Column<int>(type: "INTEGER", nullable: true),
                    IsLeaf = table.Column<bool>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: true),
                    Subtitle = table.Column<string>(type: "TEXT", nullable: true),
                    Icon = table.Column<string>(type: "TEXT", nullable: true),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    ContentMD = table.Column<string>(type: "TEXT", nullable: true),
                    Links = table.Column<string>(type: "TEXT", nullable: true),
                    TitlePictureId = table.Column<int>(type: "INTEGER", nullable: true),
                    TitleImageOffsetY = table.Column<int>(type: "INTEGER", nullable: true),
                    Meta = table.Column<string>(type: "TEXT", nullable: true),
                    ExploreStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    Lat = table.Column<double>(type: "REAL", nullable: false),
                    Lng = table.Column<double>(type: "REAL", nullable: false),
                    Zoom = table.Column<int>(type: "INTEGER", nullable: false),
                    Draft = table.Column<bool>(type: "INTEGER", nullable: false),
                    Rating = table.Column<int>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlacesNew", x => x.Id);
                });

            // migrate data from old tables
            migrationBuilder.Sql(@"
INSERT INTO PlacesNew (Id, IsLeaf, Name, Title, Subtitle, Icon,Description, ContentMD, Links, Meta, ExploreStatus, ""Order"", Lat, Lng, Zoom, Draft, Rating, UpdatedAt)
SELECT Id, TRUE, lower(p.Name), Name, Alias, Category, NULL, Notes, Links, 
       json_object('Directions', Directions, 'PublicTransport', PublicTransport, 'Season', Season, '_ParentAreaId', AreaId), 
       ExploreStatus, ""Order"", Lat, Lng, Zoom, TRUE, Rating, UpdatedAt
FROM Places p");
            migrationBuilder.Sql(
                "UPDATE SQLITE_SEQUENCE SET seq = (SELECT max(Id) + 1 FROM PlacesNew) WHERE name = 'PlacesNew'");
            migrationBuilder.Sql(@"
INSERT INTO PlacesNew (IsLeaf, Name, Title, Subtitle, Icon,Description, ContentMD, Links, Meta, ExploreStatus, ""Order"", Lat, Lng, Zoom, Draft, Rating, UpdatedAt)
SELECT FALSE, lower(Name), Name, Alias, 'star', NULL, Notes, Links,
       json_object('_ParentRegionId', RegionId, '_ThisAreaId', Id), ExploreStatus, ""Order"", Lat, Lng, Zoom, TRUE, 0, UpdatedAt FROM Areas");
            migrationBuilder.Sql(@"
INSERT INTO PlacesNew (IsLeaf, Name, Title, Subtitle, Icon,Description, ContentMD, Links, Meta, ExploreStatus, ""Order"", Lat, Lng, Zoom, Draft, Rating, UpdatedAt)
SELECT FALSE, lower(Name), Name, NULL, 'star', NULL, NULL, NULL,
       json_object('_ParentCountryId', CountryId, '_ThisRegionId', Id), 2, ""Order"", 63.1, 21.6, 0, TRUE, 0, CURRENT_TIMESTAMP FROM Regions");
            migrationBuilder.Sql(@"
INSERT INTO PlacesNew (IsLeaf, Name, Title, Subtitle, Icon,Description, ContentMD, Links, Meta, ExploreStatus, ""Order"", Lat, Lng, Zoom, Draft, Rating, UpdatedAt)
SELECT FALSE, lower(Name), Name, NULL, 'star', NULL, NULL, NULL,
       json_object('FlagEmoji', FlagEmoji, 'MapType', MapType, '_ThisCountryId', Id), 2, ""Order"", 63.1, 21.6, 0, TRUE, 0, CURRENT_TIMESTAMP FROM Countries");
            migrationBuilder.Sql(@"
UPDATE PlacesNew 
SET ParentId = (SELECT pp.Id FROM PlacesNew pp WHERE pp.Meta->>'_ThisAreaId' = PlacesNew.Meta->>'_ParentAreaId')
WHERE Meta->>'_ParentAreaId' IS NOT NULL");
            migrationBuilder.Sql(@"
UPDATE PlacesNew
SET ParentId = (SELECT pp.Id FROM PlacesNew pp WHERE pp.Meta->>'_ThisRegionId' = PlacesNew.Meta->>'_ParentRegionId')
WHERE Meta->>'_ParentRegionId' IS NOT NULL");
            migrationBuilder.Sql(@"
UPDATE PlacesNew
SET ParentId = (SELECT pp.Id FROM PlacesNew pp WHERE pp.Meta->>'_ThisCountryId' = PlacesNew.Meta->>'_ParentCountryId')
WHERE Meta->>'_ParentCountryId' IS NOT NULL");
            
            // drop old tables
            migrationBuilder.DropForeignKey(
                name: "FK_Pictures_Places_PlaceId",
                table: "Pictures");
            
            migrationBuilder.DropForeignKey(
                name: "FK_PlaceTag_Places_PlaceId",
                table: "PlaceTag");
            
            migrationBuilder.DropForeignKey(
                name: "FK_Places_Areas_AreaId",
                table: "Places");

            migrationBuilder.DropIndex(
                name: "IX_Places_Category",
                table: "Places");

            migrationBuilder.DropIndex(
                name: "IX_Places_IsPrivate",
                table: "Places");

            migrationBuilder.DropIndex(
                name: "IX_Places_Slug",
                table: "Places");
            
            migrationBuilder.DropIndex(
                name: "IX_Places_AreaId",
                table: "Places");
            
            migrationBuilder.DropTable(
                name: "PlaceLocalizations");

            migrationBuilder.DropTable(
                name: "Places");

            migrationBuilder.DropTable(
                name: "AreaLocalizations");

            migrationBuilder.DropTable(
                name: "Areas");

            migrationBuilder.DropTable(
                name: "Regions");

            migrationBuilder.DropTable(
                name: "Countries");
            
            // rename new tables and create remaining stuff
            migrationBuilder.RenameTable(name: "PlacesNew", newName: "Places");
            
            migrationBuilder.CreateIndex(
                name: "IX_Places_Icon",
                table: "Places",
                column: "Icon");

            migrationBuilder.CreateIndex(
                name: "IX_Places_Name",
                table: "Places",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Places_ExploreStatus",
                table: "Places",
                column: "ExploreStatus");
            
            migrationBuilder.CreateIndex(
                name: "IX_Places_Draft",
                table: "Places",
                column: "Draft");

            migrationBuilder.CreateIndex(
                name: "IX_Places_Rating",
                table: "Places",
                column: "Rating");
            
            migrationBuilder.CreateIndex(
                name: "IX_Places_ParentId",
                table: "Places",
                column: "ParentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Places_Places_ParentId",
                table: "Places",
                column: "ParentId",
                principalTable: "Places",
                principalColumn: "Id");
            
            migrationBuilder.AddForeignKey(
                name: "FK_PlaceTag_Places_PlacesId",
                table: "PlaceTag",
                column: "PlacesId",
                principalTable: "Places",
                principalColumn: "Id");
            
            migrationBuilder.AddForeignKey(
                name: "FK_Pictures_Places_PlaceId",
                table: "Pictures",
                column: "PlaceId",
                principalTable: "Places",
                principalColumn: "Id");
            
            migrationBuilder.CreateTable(
                name: "PlaceLocalizations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlaceId = table.Column<int>(type: "INTEGER", nullable: false),
                    Language = table.Column<string>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Subtitle = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    ContentMD = table.Column<string>(type: "TEXT", nullable: false),
                    Links = table.Column<string>(type: "TEXT", nullable: false),
                    Meta = table.Column<string>(type: "TEXT", nullable: false),
                    Draft = table.Column<bool>(type: "INTEGER", nullable: false),
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
                name: "IX_PlaceLocalizations_PlaceId",
                table: "PlaceLocalizations",
                column: "PlaceId");
            
            // triggers (missing FTS so far)
            migrationBuilder.Sql(@"
CREATE TRIGGER Places_TimestampUpdate AFTER UPDATE OF Title, Subtitle, Description, CommentMD, Links, Meta ON Places BEGIN
    UPDATE Places SET UpdatedAt = CURRENT_TIMESTAMP WHERE Id = new.Id;
END");
            migrationBuilder.Sql(@"
CREATE TRIGGER Places_TimestampInsert AFTER INSERT ON Places BEGIN
    UPDATE Places SET UpdatedAt = CURRENT_TIMESTAMP WHERE Id = new.Id;
END");        
            migrationBuilder.Sql(@"
CREATE TRIGGER PlaceLocalizations_TimestampUpdate AFTER UPDATE OF Title, Subtitle, Description, CommentMD, Links, Meta ON PlaceLocalizations BEGIN
    UPDATE PlaceLocalizations SET UpdatedAt = CURRENT_TIMESTAMP WHERE Id = new.Id;
END");
            migrationBuilder.Sql(@"
CREATE TRIGGER PlaceLocalizations_TimestampInsert AFTER INSERT ON PlaceLocalizations BEGIN
    UPDATE PlaceLocalizations SET UpdatedAt = CURRENT_TIMESTAMP WHERE Id = new.Id;
END");        
        
            migrationBuilder.Sql("PRAGMA foreign_keys = ON;", suppressTransaction: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotImplementedException("Cannot revert this one.  Sorry");
        }
    }
}
