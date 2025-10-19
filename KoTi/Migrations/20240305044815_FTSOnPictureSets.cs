using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KoTi.Migrations
{
    /// <inheritdoc />
    public partial class FTSOnPictureSets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // triggers for regions
            migrationBuilder.Sql(@"
CREATE TRIGGER PictureSets_SearchInsert AFTER INSERT ON PictureSets BEGIN
    INSERT INTO Search(TableName, TableId, Title, Text)
        SELECT 'PictureSets', ps.Id, CASE
            WHEN ps.ParentId IS NULL THEN ps.Name
            WHEN ps.ParentId IS NOT NULL AND pps.ParentId IS NULL THEN pps.Name || ' > ' || ps.Name
            ELSE '... > ' || pps.Name || ' > ' || ps.Name
            END, '' FROM main.PictureSets ps LEFT OUTER JOIN main.PictureSets pps ON pps.Id = ps.ParentId
            WHERE ps.Id = new.Id;
END");
            migrationBuilder.Sql(@"
CREATE TRIGGER PictureSets_SearchDelete AFTER DELETE ON PictureSets BEGIN
    DELETE FROM Search WHERE TableName = 'PictureSets' AND TableId = old.Id;
END");
            migrationBuilder.Sql(@"
CREATE TRIGGER PictureSets_SearchUpdate AFTER UPDATE ON PictureSets BEGIN
    DELETE FROM Search WHERE TableName = 'PictureSets' AND TableId = old.Id;
    INSERT INTO Search(TableName, TableId, Title, Text)
        SELECT 'PictureSets', ps.Id, CASE
            WHEN ps.ParentId IS NULL THEN ps.Name
            WHEN ps.ParentId IS NOT NULL AND pps.ParentId IS NULL THEN pps.Name || ' > ' || ps.Name
            ELSE '... > ' || pps.Name || ' > ' || ps.Name
            END, '' FROM main.PictureSets ps LEFT OUTER JOIN main.PictureSets pps ON pps.Id = ps.ParentId
            WHERE ps.Id = new.Id;
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS PictureSets_SearchInsert");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS PictureSets_SearchDelete");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS PictureSets_SearchUpdate");
        }
    }
}
