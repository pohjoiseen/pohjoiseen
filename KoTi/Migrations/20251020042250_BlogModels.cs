using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KoTi.Migrations
{
    /// <inheritdoc />
    public partial class BlogModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Articles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Draft = table.Column<bool>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    ContentMD = table.Column<string>(type: "TEXT", nullable: false),
                    Language = table.Column<string>(type: "TEXT", maxLength: 2, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Articles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Posts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Draft = table.Column<bool>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    ContentMD = table.Column<string>(type: "TEXT", nullable: false),
                    Language = table.Column<string>(type: "TEXT", maxLength: 2, nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Mini = table.Column<bool>(type: "INTEGER", nullable: false),
                    TitlePictureId = table.Column<int>(type: "INTEGER", nullable: true),
                    TitleImage = table.Column<string>(type: "TEXT", nullable: true),
                    TitleImageOffsetY = table.Column<int>(type: "INTEGER", nullable: true),
                    TitleImageInText = table.Column<bool>(type: "INTEGER", nullable: true),
                    TitleImageCaption = table.Column<string>(type: "TEXT", nullable: true),
                    DateDescription = table.Column<string>(type: "TEXT", nullable: true),
                    LocationDescription = table.Column<string>(type: "TEXT", nullable: true),
                    Address = table.Column<string>(type: "TEXT", nullable: true),
                    PublicTransport = table.Column<string>(type: "TEXT", nullable: true),
                    Twitter = table.Column<string>(type: "TEXT", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CoatsOfArms = table.Column<string>(type: "TEXT", nullable: true),
                    Geo = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Posts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Posts_Pictures_TitlePictureId",
                        column: x => x.TitlePictureId,
                        principalTable: "Pictures",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Articles_Name",
                table: "Articles",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Posts_TitlePictureId",
                table: "Posts",
                column: "TitlePictureId");
            
            migrationBuilder.Sql(@"
CREATE TRIGGER Posts_SearchInsert AFTER INSERT ON Posts BEGIN
    INSERT INTO Search(TableName, TableId, Title, Text)
    SELECT 'Posts', p.Id, p.Name, p.Name || char(10) || 
        p.Title || char(10) ||
        CASE WHEN p.Description IS NOT NULL THEN p.Description || char(10) ELSE '' END ||
        CASE WHEN p.DateDescription IS NOT NULL THEN 'Date:' || p.DateDescription || char(10) ELSE '' END || 
        CASE WHEN p.LocationDescription IS NOT NULL THEN 'Location: ' || p.LocationDescription || char(10) ELSE '' END ||
        CASE WHEN p.Address IS NOT NULL THEN 'Address: ' || p.Address || char(10) ELSE '' END ||
        CASE WHEN p.PublicTransport IS NOT NULL THEN 'Public transport: ' || p.PublicTransport || char(10) ELSE '' END || 
        coalesce('Geo: ' || group_concat(json_extract(g.value, '$.Title') || ' ' || coalesce(json_extract(g.value, '$.Subtitle') || ' ', '') || coalesce(json_extract(g.value, '$.Description'), '')) || char(10), '') ||  
        p.ContentMD || char(10) 
    FROM Posts p
    LEFT JOIN json_each(p.Geo) AS g
    WHERE p.Id = new.Id
    GROUP BY p.Id;
END");
            migrationBuilder.Sql(@"
CREATE TRIGGER Posts_SearchUpdate AFTER UPDATE ON Posts BEGIN
    DELETE FROM Search WHERE TableName = 'Posts' AND TableId = old.Id;
    INSERT INTO Search(TableName, TableId, Title, Text)
    SELECT 'Posts', p.Id, p.Name, p.Name || char(10) || 
        p.Title || char(10) ||
        CASE WHEN p.Description IS NOT NULL THEN p.Description || char(10) ELSE '' END ||
        CASE WHEN p.DateDescription IS NOT NULL THEN 'Date:'  || p.DateDescription || char(10) ELSE '' END || 
        CASE WHEN p.LocationDescription IS NOT NULL THEN 'Location: ' || p.LocationDescription || char(10) ELSE '' END ||
        CASE WHEN p.Address IS NOT NULL THEN 'Address: ' || p.Address || char(10) ELSE '' END ||
        CASE WHEN p.PublicTransport IS NOT NULL THEN 'Public transport: ' || p.PublicTransport || char(10) ELSE '' END || 
        coalesce('Geo: ' || group_concat(json_extract(g.value, '$.Title') || ' ' || coalesce(json_extract(g.value, '$.Subtitle') || ' ', '') || coalesce(json_extract(g.value, '$.Description'), '')) || char(10), '') ||  
        p.ContentMD || char(10) 
    FROM Posts p
    LEFT JOIN json_each(p.Geo) AS g
    WHERE p.Id = new.Id
    GROUP BY p.Id;
END");
            migrationBuilder.Sql(@"
CREATE TRIGGER Posts_SearchDelete AFTER DELETE ON Posts BEGIN
    DELETE FROM Search WHERE TableName = 'Posts' AND TableId = old.Id;
END");
            migrationBuilder.Sql(@"
CREATE TRIGGER Posts_TimestampInsert AFTER INSERT ON Posts BEGIN
    UPDATE Posts SET UpdatedAt = CURRENT_TIMESTAMP WHERE Id = new.Id;
END");
            migrationBuilder.Sql(@"
CREATE TRIGGER Posts_TimestampUpdate AFTER UPDATE ON Posts BEGIN
    UPDATE Posts SET UpdatedAt = CURRENT_TIMESTAMP WHERE Id = new.Id;
END");
            
            migrationBuilder.Sql(@"
CREATE TRIGGER Articles_SearchInsert AFTER INSERT ON Articles BEGIN
    INSERT INTO Search(TableName, TableId, Title, Text)
    VALUES ('Articles', new.Id, new.Name, new.Name || char(10) || new.ContentMD);
END");
            migrationBuilder.Sql(@"
CREATE TRIGGER Articles_SearchUpdate AFTER UPDATE ON Articles BEGIN
    DELETE FROM Search WHERE TableName = 'Articles' AND TableId = old.Id;
    INSERT INTO Search(TableName, TableId, Title, Text)
    VALUES ('Articles', new.Id, new.Name, new.Name || char(10) || new.ContentMD);
END");
            migrationBuilder.Sql(@"
CREATE TRIGGER Articles_SearchDelete AFTER DELETE ON Articles BEGIN
    DELETE FROM Search WHERE TableName = 'Articles' AND TableId = old.Id;
END");
            migrationBuilder.Sql(@"
CREATE TRIGGER Articles_TimestampInsert AFTER INSERT ON Articles BEGIN
    UPDATE Articles SET UpdatedAt = CURRENT_TIMESTAMP WHERE Id = new.Id;
END");
            migrationBuilder.Sql(@"
CREATE TRIGGER Articles_TimestampUpdate AFTER UPDATE ON Articles BEGIN
    UPDATE Articles SET UpdatedAt = CURRENT_TIMESTAMP WHERE Id = new.Id;
END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS Articles_SearchInsert");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS Articles_SearchUpdate");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS Articles_SearchDelete");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS Articles_TimestampInsert");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS Articles_TimestampUpdate");

            migrationBuilder.Sql("DROP TRIGGER IF EXISTS Posts_SearchInsert");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS Posts_SearchUpdate");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS Posts_SearchDelete");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS Posts_TimestampInsert");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS Posts_TimestampUpdate");

            migrationBuilder.DropTable(
                name: "Articles");

            migrationBuilder.DropTable(
                name: "Posts");
        }
    }
}
