using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Holvi.Migrations
{
    /// <inheritdoc />
    public partial class Search : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // create full-text search table
            // this is NOT an external-content table although that would have made sense,
            // but I had difficulty implementing that at least when actually using content from multiple tables
            migrationBuilder.Sql("CREATE VIRTUAL TABLE Search USING fts5 (TableName, TableId, Title, Text, tokenize = porter)");
            
            // populate table initially
            migrationBuilder.Sql(@"
INSERT INTO Search(TableName, TableId, Title, Text)
SELECT 'Pictures', Id, COALESCE(Title, Filename), Description
FROM Pictures
UNION ALL
SELECT 'Places', Id, Name,
       Alias || char(10) ||
       Notes || char(10) ||
       CASE WHEN length(Directions) > 0 THEN 'Address/directions: ' || Places.Directions || char(10) ELSE '' END ||
       CASE WHEN length(PublicTransport) > 0 THEN 'Public transport: ' || Places.PublicTransport || char(10) ELSE '' END ||
       CASE WHEN length(Season) > 0 THEN 'Season: ' || Places.Season || char(10) ELSE '' END ||
       CASE WHEN length(Links) > 0 THEN 'Links: ' || Places.Links ELSE '' END
FROM Places
UNION ALL
SELECT 'Areas', Id, Name, Notes || char(10) || Links FROM Areas
UNION ALL
SELECT 'Regions', Id, Name, '' FROM Regions
UNION ALL
SELECT 'Countries', Id, Name, '' FROM Countries
");
            
            // triggers for pictures
            migrationBuilder.Sql(@"
CREATE TRIGGER Pictures_SearchInsert AFTER INSERT ON Pictures BEGIN
    INSERT INTO Search(TableName, TableId, Title, Text)
        VALUES ('Pictures', new.Id, COALESCE(new.Title, new.Filename), new.Description);
END");
            migrationBuilder.Sql(@"
CREATE TRIGGER Pictures_SearchDelete AFTER DELETE ON Pictures BEGIN
    DELETE FROM Search WHERE TableName = 'Pictures' AND TableId = old.Id;
END");
            migrationBuilder.Sql(@"
CREATE TRIGGER Pictures_SearchUpdate AFTER UPDATE ON Pictures BEGIN
    DELETE FROM Search WHERE TableName = 'Pictures' AND TableId = old.Id;
    INSERT INTO Search(TableName, TableId, Title, Text)
        VALUES ('Pictures', new.Id, COALESCE(new.Title, new.Filename), new.Description);
END");
            
            // triggers for places
            migrationBuilder.Sql(@"
CREATE TRIGGER Places_SearchInsert AFTER INSERT ON Places BEGIN
    INSERT INTO Search(TableName, TableId, Title, Text)
    VALUES ('Places', new.Id, new.Name, new.Alias || char(10) ||
                                        new.Notes || char(10) ||
                                        CASE WHEN length(new.Directions) > 0 THEN 'Address/directions: ' || new.Directions || char(10) ELSE '' END ||
                                        CASE WHEN length(new.PublicTransport) > 0 THEN 'Public transport: ' || new.PublicTransport || char(10) ELSE '' END ||
                                        CASE WHEN length(new.Season) > 0 THEN 'Season: ' || new.Season || char(10) ELSE '' END ||
                                        CASE WHEN length(new.Links) > 0 THEN 'Links: ' || new.Links ELSE '' END);
END");
            migrationBuilder.Sql(@"
CREATE TRIGGER Places_SearchDelete AFTER DELETE ON Places BEGIN
    DELETE FROM Search WHERE TableName = 'Places' AND TableId = old.Id;
END");
            migrationBuilder.Sql(@"
CREATE TRIGGER Places_SearchUpdate AFTER UPDATE ON Places BEGIN
    DELETE FROM Search WHERE TableName = 'Places' AND TableId = old.Id;
    INSERT INTO Search(TableName, TableId, Title, Text)
    VALUES ('Places', new.Id, new.Name, new.Alias || char(10) ||
                                        new.Notes || char(10) ||
                                        CASE WHEN length(new.Directions) > 0 THEN 'Address/directions: ' || new.Directions || char(10) ELSE '' END ||
                                        CASE WHEN length(new.PublicTransport) > 0 THEN 'Public transport: ' || new.PublicTransport || char(10) ELSE '' END ||
                                        CASE WHEN length(new.Season) > 0 THEN 'Season: ' || new.Season || char(10) ELSE '' END ||
                                        CASE WHEN length(new.Links) > 0 THEN 'Links: ' || new.Links ELSE '' END);

END");

            // triggers for areas
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
END");
            
            // triggers for regions
            migrationBuilder.Sql(@"
CREATE TRIGGER Regions_SearchInsert AFTER INSERT ON Regions BEGIN
    INSERT INTO Search(TableName, TableId, Title, Text)
        VALUES ('Regions', new.Id, new.Name, '');
END");
            migrationBuilder.Sql(@"
CREATE TRIGGER Regions_SearchDelete AFTER DELETE ON Regions BEGIN
    DELETE FROM Search WHERE TableName = 'Regions' AND TableId = old.Id;
END");
            migrationBuilder.Sql(@"
CREATE TRIGGER Regions_SearchUpdate AFTER UPDATE ON Regions BEGIN
    DELETE FROM Search WHERE TableName = 'Regions' AND TableId = old.Id;
    INSERT INTO Search(TableName, TableId, Title, Text)
        VALUES ('Regions', new.Id, new.Name, '');
END");
            
            // triggers for countries
            migrationBuilder.Sql(@"
CREATE TRIGGER Countries_SearchInsert AFTER INSERT ON Countries BEGIN
    INSERT INTO Search(TableName, TableId, Title, Text)
        VALUES ('Countries', new.Id, new.Name, '');
END");
            migrationBuilder.Sql(@"
CREATE TRIGGER Countries_SearchDelete AFTER DELETE ON Countries BEGIN
    DELETE FROM Search WHERE TableName = 'Countries' AND TableId = old.Id;
END");
            migrationBuilder.Sql(@"
CREATE TRIGGER Countries_SearchUpdate AFTER UPDATE ON Countries BEGIN
    DELETE FROM Search WHERE TableName = 'Countries' AND TableId = old.Id;
    INSERT INTO Search(TableName, TableId, Title, Text)
        VALUES ('Countries', new.Id, new.Name, '');
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS Pictures_SearchInsert");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS Pictures_SearchDelete");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS Pictures_SearchUpdate");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS Places_SearchInsert");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS Places_SearchDelete");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS Places_SearchUpdate");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS Areas_SearchInsert");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS Areas_SearchDelete");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS Areas_SearchUpdate");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS Regions_SearchInsert");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS Regions_SearchDelete");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS Regions_SearchUpdate");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS Countries_SearchInsert");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS Countries_SearchDelete");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS Countries_SearchUpdate");
            migrationBuilder.Sql("DROP TABLE IF EXISTS Search");
        }
    }
}
