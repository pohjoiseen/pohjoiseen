using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Holvi.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // this is big and annoying because we need to drop and recreate affected tables, with indices and triggers
            // because sqlite has limited ALTER TABLE support and does not allow to add a new column with a default of CURRENT_TIMESTAMP
            
            // note also that we set update triggers only on specific columns; the idea is that just reordering of things
            // or altering other stuff that is not factual but just rating/explore status/etc. will not update timestamp
            
            // suppressTransaction=true feels bad, but at least during development it didn't seem to case any troubles
            // when migration was failing...
            migrationBuilder.Sql(@"PRAGMA foreign_keys=OFF", true);
            
            migrationBuilder.Sql("DROP INDEX IX_Areas_RegionId");
            migrationBuilder.Sql("DROP TRIGGER Areas_SearchInsert");
            migrationBuilder.Sql("DROP TRIGGER Areas_SearchDelete");
            migrationBuilder.Sql("DROP TRIGGER Areas_SearchUpdate");
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS ""Areas_tmp"" (
    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_Areas"" PRIMARY KEY AUTOINCREMENT,
    ""RegionId"" INTEGER NOT NULL,
    ""Name"" TEXT NOT NULL,
    ""Notes"" TEXT NOT NULL,
    ""Links"" TEXT NOT NULL,
    ""ExploreStatus"" INTEGER NOT NULL,
    ""Order"" INTEGER NOT NULL, 
    ""Lat"" REAL NOT NULL DEFAULT 0.0,
    ""Lng"" REAL NOT NULL DEFAULT 0.0,
    ""Zoom"" INTEGER NOT NULL DEFAULT 0,
    ""UpdatedAt"" TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT ""FK_Areas_Regions_RegionId"" FOREIGN KEY (""RegionId"") REFERENCES ""Regions"" (""Id"") ON DELETE CASCADE
)");
            migrationBuilder.Sql(@"INSERT INTO Areas_tmp (Id, RegionId, Name, Notes, Links, ExploreStatus, ""Order"", Lat, Lng, Zoom) SELECT * FROM Areas");

            migrationBuilder.Sql("DROP INDEX IX_Places_AreaId");
            migrationBuilder.Sql("DROP INDEX IX_Places_Category");
            migrationBuilder.Sql("DROP INDEX IX_Places_ExploreStatus");
            migrationBuilder.Sql("DROP INDEX IX_Places_IsPrivate");
            migrationBuilder.Sql("DROP INDEX IX_Places_Rating");
            migrationBuilder.Sql("DROP TRIGGER Places_SearchInsert");
            migrationBuilder.Sql("DROP TRIGGER Places_SearchDelete");
            migrationBuilder.Sql("DROP TRIGGER Places_SearchUpdate");
            migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS ""Places_tmp"" (
    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_Places"" PRIMARY KEY AUTOINCREMENT,
    ""AreaId"" INTEGER NOT NULL,
    ""Name"" TEXT NOT NULL,
    ""Notes"" TEXT NOT NULL,
    ""Links"" TEXT NOT NULL,
    ""ExploreStatus"" INTEGER NOT NULL,
    ""Order"" INTEGER NOT NULL, 
    ""Category"" TEXT NOT NULL DEFAULT '', 
    ""Directions"" TEXT NOT NULL DEFAULT '', 
    ""PublicTransport"" TEXT NOT NULL DEFAULT '', 
    ""Season"" TEXT NOT NULL DEFAULT '', 
    ""Alias"" TEXT NOT NULL DEFAULT '', 
    ""Lat"" REAL NOT NULL DEFAULT 0.0,
    ""Lng"" REAL NOT NULL DEFAULT 0.0, 
    ""Zoom"" INTEGER NOT NULL DEFAULT 0,
    ""IsPrivate"" INTEGER NOT NULL DEFAULT 0, 
    ""Rating"" INTEGER NOT NULL DEFAULT 0,
    ""UpdatedAt"" TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT ""FK_Places_Areas_AreaId"" FOREIGN KEY (""AreaId"") REFERENCES ""Areas"" (""Id"") ON DELETE CASCADE
);");
            migrationBuilder.Sql(@"INSERT INTO Places_tmp (Id, AreaId, Name, Notes, Links, ExploreStatus, ""Order"", Category, Directions, PublicTransport, Season, Alias, Lat, Lng, Zoom, IsPrivate, Rating) SELECT * FROM Places");

            migrationBuilder.Sql(@"DROP INDEX IX_Pictures_PlaceId");
            migrationBuilder.Sql(@"DROP INDEX IX_Pictures_SetId");
            migrationBuilder.Sql(@"DROP INDEX IX_Pictures_Hash");
            migrationBuilder.Sql(@"DROP INDEX IX_Pictures_IsPrivate");
            migrationBuilder.Sql(@"DROP INDEX IX_Pictures_PhotographedAt");
            migrationBuilder.Sql(@"DROP INDEX IX_Pictures_Rating");
            migrationBuilder.Sql(@"CREATE TABLE IF NOT EXISTS ""Pictures_tmp"" (
    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_Pictures"" PRIMARY KEY AUTOINCREMENT,
    ""Camera"" TEXT NULL,
    ""Description"" TEXT NOT NULL,
    ""DetailsUrl"" TEXT NOT NULL,
    ""Filename"" TEXT NOT NULL,
    ""Hash"" TEXT NOT NULL,
    ""Height"" INTEGER NOT NULL,
    ""IsPrivate"" INTEGER NOT NULL,
    ""Lat"" REAL NULL,
    ""Lens"" TEXT NULL,
    ""Lng"" REAL NULL,
    ""PhotographedAt"" TEXT NULL,
    ""PlaceId"" INTEGER NULL,
    ""SetId"" INTEGER NULL,
    ""Size"" INTEGER NOT NULL,
    ""ThumbnailUrl"" TEXT NOT NULL,
    ""Title"" TEXT NOT NULL,
    ""UploadedAt"" TEXT NOT NULL,
    ""Url"" TEXT NOT NULL,
    ""Width"" INTEGER NOT NULL, 
    ""Rating"" INTEGER NOT NULL DEFAULT 0,
    ""UpdatedAt"" TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT ""FK_Pictures_PictureSets_SetId"" FOREIGN KEY (""SetId"") REFERENCES ""PictureSets"" (""Id""),
    CONSTRAINT ""FK_Pictures_Places_PlaceId"" FOREIGN KEY (""PlaceId"") REFERENCES ""Places"" (""Id"")
);");
            migrationBuilder.Sql(@"INSERT INTO Pictures_tmp (Id, Camera, Description, DetailsUrl, Filename, Hash, Height, IsPrivate, Lat, Lens, Lng, PhotographedAt,
                                                             PlaceId, SetId, Size, ThumbnailUrl, Title, UploadedAt, Url, Width, Rating) SELECT * FROM Pictures");
            
            migrationBuilder.DropTable("Pictures");
            migrationBuilder.RenameTable(name: "Pictures_tmp", newName: "Pictures");
            migrationBuilder.DropTable("Places");
            migrationBuilder.RenameTable(name: "Places_tmp", newName: "Places");
            migrationBuilder.DropTable("Areas");
            migrationBuilder.RenameTable(name: "Areas_tmp", newName: "Areas");
            
            migrationBuilder.Sql(@"CREATE INDEX ""IX_Areas_RegionId"" ON ""Areas"" (""RegionId"");");
            migrationBuilder.Sql(@"
CREATE TRIGGER Areas_SearchInsert AFTER INSERT ON Areas BEGIN
    INSERT INTO Search(TableName, TableId, Title, Text)
        VALUES ('Areas', new.Id, new.Name, new.Notes || char(10) || new.Links);
END");
            migrationBuilder.Sql(@"
CREATE TRIGGER Areas_SearchDelete AFTER DELETE ON Areas BEGIN
    DELETE FROM Search WHERE TableName = 'Areas' AND TableId = old.Id;
END");
            migrationBuilder.Sql(@"
CREATE TRIGGER Areas_SearchUpdate AFTER UPDATE ON Areas BEGIN
    DELETE FROM Search WHERE TableName = 'Areas' AND TableId = old.Id;
    INSERT INTO Search(TableName, TableId, Title, Text)
        VALUES ('Areas', new.Id, new.Name, new.Notes || char(10) || new.Links);
END;");
            migrationBuilder.Sql(@"
CREATE TRIGGER Areas_TimestampUpdate AFTER UPDATE OF RegionId, Name, Notes, Links, Lat, Lng, Zoom ON Areas BEGIN
    UPDATE Areas SET UpdatedAt = CURRENT_TIMESTAMP WHERE Id = new.Id;
END");
            migrationBuilder.Sql(@"
CREATE TRIGGER Areas_TimestampInsert AFTER INSERT ON Areas BEGIN
    UPDATE Areas SET UpdatedAt = CURRENT_TIMESTAMP WHERE Id = new.Id;
END");
            
            migrationBuilder.Sql(@"CREATE INDEX ""IX_Places_AreaId"" ON ""Places"" (""AreaId"")");
            migrationBuilder.Sql(@"CREATE TRIGGER Places_SearchInsert AFTER INSERT ON Places BEGIN
    INSERT INTO Search(TableName, TableId, Title, Text)
    VALUES ('Places', new.Id, new.Name, new.Alias || char(10) ||
                                        new.Notes || char(10) ||
                                        CASE WHEN length(new.Directions) > 0 THEN 'Address/directions: ' || new.Directions || char(10) ELSE '' END ||
                                        CASE WHEN length(new.PublicTransport) > 0 THEN 'Public transport: ' || new.PublicTransport || char(10) ELSE '' END ||
                                        CASE WHEN length(new.Season) > 0 THEN 'Season: ' || new.Season || char(10) ELSE '' END ||
                                        CASE WHEN length(new.Links) > 0 THEN 'Links: ' || new.Links ELSE '' END);
END");
            migrationBuilder.Sql(@"CREATE TRIGGER Places_SearchDelete AFTER DELETE ON Places BEGIN
    DELETE FROM Search WHERE TableName = 'Places' AND TableId = old.Id;
END");
            migrationBuilder.Sql(@"CREATE TRIGGER Places_SearchUpdate AFTER UPDATE ON Places BEGIN
    DELETE FROM Search WHERE TableName = 'Places' AND TableId = old.Id;
    INSERT INTO Search(TableName, TableId, Title, Text)
    VALUES ('Places', new.Id, new.Name, new.Alias || char(10) ||
                                        new.Notes || char(10) ||
                                        CASE WHEN length(new.Directions) > 0 THEN 'Address/directions: ' || new.Directions || char(10) ELSE '' END ||
                                        CASE WHEN length(new.PublicTransport) > 0 THEN 'Public transport: ' || new.PublicTransport || char(10) ELSE '' END ||
                                        CASE WHEN length(new.Season) > 0 THEN 'Season: ' || new.Season || char(10) ELSE '' END ||
                                        CASE WHEN length(new.Links) > 0 THEN 'Links: ' || new.Links ELSE '' END);

END");
            migrationBuilder.Sql(@"CREATE INDEX ""IX_Places_Category"" ON ""Places"" (""Category"")");
            migrationBuilder.Sql(@"CREATE INDEX ""IX_Places_ExploreStatus"" ON ""Places"" (""ExploreStatus"")");
            migrationBuilder.Sql(@"CREATE INDEX ""IX_Places_IsPrivate"" ON ""Places"" (""IsPrivate"")");
            migrationBuilder.Sql(@"CREATE INDEX ""IX_Places_Rating"" ON ""Places"" (""Rating"")");
            migrationBuilder.Sql(@"
CREATE TRIGGER Places_TimestampUpdate AFTER UPDATE OF AreaId, Name, Notes, Links, Category, Directions, PublicTransport, Season, Alias, Lat, Lng, Zoom ON Places BEGIN
    UPDATE Places SET UpdatedAt = CURRENT_TIMESTAMP WHERE Id = new.Id;
END");
            migrationBuilder.Sql(@"
CREATE TRIGGER Places_TimestampInsert AFTER INSERT ON Places BEGIN
    UPDATE Places SET UpdatedAt = CURRENT_TIMESTAMP WHERE Id = new.Id;
END");
            
            
            migrationBuilder.Sql(@"CREATE INDEX ""IX_Pictures_PlaceId"" ON ""Pictures"" (""PlaceId"")");
            migrationBuilder.Sql(@"CREATE INDEX ""IX_Pictures_SetId"" ON ""Pictures"" (""SetId"")");
            migrationBuilder.Sql(@"CREATE UNIQUE INDEX ""IX_Pictures_Hash"" ON ""Pictures"" (""Hash"")");
            migrationBuilder.Sql(@"CREATE INDEX ""IX_Pictures_IsPrivate"" ON ""Pictures"" (""IsPrivate"")");
            migrationBuilder.Sql(@"CREATE INDEX ""IX_Pictures_PhotographedAt"" ON ""Pictures"" (""PhotographedAt"")");
            migrationBuilder.Sql(@"CREATE INDEX ""IX_Pictures_Rating"" ON ""Pictures"" (""Rating"")");
            migrationBuilder.Sql(@"
CREATE TRIGGER Pictures_TimestampUpdate AFTER UPDATE OF Camera, Description, DetailsUrl, Filename, Hash, Height, Lat, Lens, Lng,
                                                        PhotographedAt, Size, ThumbnailUrl, Title, UploadedAt, Url, Width ON Pictures BEGIN
    UPDATE Pictures SET UpdatedAt = CURRENT_TIMESTAMP WHERE Id = new.Id;
END");
            migrationBuilder.Sql(@"
CREATE TRIGGER Pictures_TimestampInsert AFTER INSERT ON Pictures BEGIN
    UPDATE Pictures SET UpdatedAt = CURRENT_TIMESTAMP WHERE Id = new.Id;
END");
            
            migrationBuilder.Sql(@"PRAGMA foreign_key_check", true);
            migrationBuilder.Sql(@"PRAGMA foreign_keys=ON", true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Places");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Pictures");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Areas");

            migrationBuilder.Sql("DROP TRIGGER IF EXISTS Areas_TimestampUpdate");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS Areas_TimestampInsert");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS Places_TimestampUpdate");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS Places_TimestampInsert");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS Pictures_TimestampUpdate");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS Pictures_TimestampInsert");
        }
    }
}
