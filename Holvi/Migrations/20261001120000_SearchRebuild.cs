using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Holvi.Migrations
{
    /// <summary>
    /// Rebuild full-text search from scratch:
    /// - switch tokenizer from porter (English-only stemming) to unicode61 with diacritics folding
    ///   (ä = a etc.; only for Latin script though, ё and е are still different), and case folding also for Cyrillic
    /// - drop index content and triggers for long gone tables (Places, Areas, Regions, Countries) and
    ///   for Pictures (not searched anymore, triggers were lost long ago anyway)
    /// - recreate triggers for Posts, they were silently dropped when EF rebuilt the table in Books migration
    ///   (NB: SQLite table rebuilds by EF, e.g. on AddForeignKey/DropForeignKey, drop all triggers on the table!
    ///   Any migration doing that to Posts, Articles, Books or PictureSets must recreate the triggers below)
    /// - add triggers for Books, these were never there
    /// - drop timestamp triggers, UpdatedAt is now set in HolviDbContext.SaveChanges
    /// </summary>
    public partial class SearchRebuild : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            Rebuild(migrationBuilder, "'unicode61 remove_diacritics 2'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // not a full restore of the previous (broken) state, just go back to porter tokenizer
            Rebuild(migrationBuilder, "porter");
        }

        private static readonly string[] OldTriggers =
        [
            "Pictures_SearchInsert", "Pictures_SearchDelete", "Pictures_SearchUpdate",
            "Pictures_TimestampInsert", "Pictures_TimestampUpdate",
            "Places_SearchInsert", "Places_SearchDelete", "Places_SearchUpdate",
            "Places_TimestampInsert", "Places_TimestampUpdate",
            "Areas_SearchInsert", "Areas_SearchDelete", "Areas_SearchUpdate",
            "Areas_TimestampInsert", "Areas_TimestampUpdate",
            "Regions_SearchInsert", "Regions_SearchDelete", "Regions_SearchUpdate",
            "Countries_SearchInsert", "Countries_SearchDelete", "Countries_SearchUpdate",
            "PictureSets_SearchInsert", "PictureSets_SearchDelete", "PictureSets_SearchUpdate",
            "Posts_SearchInsert", "Posts_SearchDelete", "Posts_SearchUpdate",
            "Posts_TimestampInsert", "Posts_TimestampUpdate",
            "Articles_SearchInsert", "Articles_SearchDelete", "Articles_SearchUpdate",
            "Articles_TimestampInsert", "Articles_TimestampUpdate",
            "Books_SearchInsert", "Books_SearchDelete", "Books_SearchUpdate",
        ];

        // Search row contents per table, as a SELECT from the table aliased as "t", for given id expression
        // (or all rows when null).  Used both for initial population and in triggers.
        private static string SelectPosts(string id) => $@"
    SELECT 'Posts', t.Id, t.Name, t.Name || char(10) ||
        t.Title || char(10) ||
        CASE WHEN t.Description IS NOT NULL THEN t.Description || char(10) ELSE '' END ||
        CASE WHEN t.DateDescription IS NOT NULL THEN 'Date: ' || t.DateDescription || char(10) ELSE '' END ||
        CASE WHEN t.LocationDescription IS NOT NULL THEN 'Location: ' || t.LocationDescription || char(10) ELSE '' END ||
        CASE WHEN t.Address IS NOT NULL THEN 'Address: ' || t.Address || char(10) ELSE '' END ||
        CASE WHEN t.PublicTransport IS NOT NULL THEN 'Public transport: ' || t.PublicTransport || char(10) ELSE '' END ||
        coalesce('Geo: ' || group_concat(json_extract(g.value, '$.Title') || ' ' || coalesce(json_extract(g.value, '$.Subtitle') || ' ', '') || coalesce(json_extract(g.value, '$.Description'), '')) || char(10), '') ||
        t.ContentMD || char(10)
    FROM main.Posts t
    LEFT JOIN json_each(t.Geo) AS g
    {(id is null ? "" : $"WHERE t.Id = {id}")}
    GROUP BY t.Id";

        private static string SelectArticles(string id) => $@"
    SELECT 'Articles', t.Id, t.Name, t.Name || char(10) || t.Title || char(10) || t.ContentMD
    FROM main.Articles t
    {(id is null ? "" : $"WHERE t.Id = {id}")}";

        private static string SelectBooks(string id) => $@"
    SELECT 'Books', t.Id, t.Name, t.Name || char(10) || t.Title || char(10) || t.ContentMD
    FROM main.Books t
    {(id is null ? "" : $"WHERE t.Id = {id}")}";

        private static string SelectPictureSets(string id) => $@"
    SELECT 'PictureSets', t.Id, CASE
        WHEN t.ParentId IS NULL THEN t.Name
        WHEN t.ParentId IS NOT NULL AND pt.ParentId IS NULL THEN pt.Name || ' > ' || t.Name
        ELSE '... > ' || pt.Name || ' > ' || t.Name
        END, ''
    FROM main.PictureSets t
    LEFT OUTER JOIN main.PictureSets pt ON pt.Id = t.ParentId
    {(id is null ? "" : $"WHERE t.Id = {id}")}";

        private static void Rebuild(MigrationBuilder migrationBuilder, string tokenizer)
        {
            foreach (var trigger in OldTriggers)
            {
                migrationBuilder.Sql($"DROP TRIGGER IF EXISTS {trigger}");
            }

            migrationBuilder.Sql("DROP TABLE IF EXISTS Search");
            migrationBuilder.Sql($"CREATE VIRTUAL TABLE Search USING fts5 (TableName UNINDEXED, TableId UNINDEXED, Title, Text, tokenize = {tokenizer})");

            var tables = new (string Name, Func<string, string> Select)[]
            {
                ("Posts", SelectPosts),
                ("Articles", SelectArticles),
                ("Books", SelectBooks),
                ("PictureSets", SelectPictureSets),
            };
            foreach (var (name, select) in tables)
            {
                migrationBuilder.Sql($"INSERT INTO Search(TableName, TableId, Title, Text) {select(null)}");
                migrationBuilder.Sql($@"
CREATE TRIGGER {name}_SearchInsert AFTER INSERT ON {name} BEGIN
    INSERT INTO Search(TableName, TableId, Title, Text) {select("new.Id")};
END");
                migrationBuilder.Sql($@"
CREATE TRIGGER {name}_SearchUpdate AFTER UPDATE ON {name} BEGIN
    DELETE FROM Search WHERE TableName = '{name}' AND TableId = old.Id;
    INSERT INTO Search(TableName, TableId, Title, Text) {select("new.Id")};
END");
                migrationBuilder.Sql($@"
CREATE TRIGGER {name}_SearchDelete AFTER DELETE ON {name} BEGIN
    DELETE FROM Search WHERE TableName = '{name}' AND TableId = old.Id;
END");
            }
        }
    }
}
