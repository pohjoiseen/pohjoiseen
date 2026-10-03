using Holvi;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Tests.Infrastructure;

/// <summary>
/// Creates SQLite databases for tests: a template is created once per test run by applying all migrations
/// (so search triggers etc. are exactly like in production) and seeding <see cref="TestData"/>,
/// each test (class) then gets its own copy.
/// </summary>
public static class TestDatabase
{
    private static readonly string Directory = Path.Combine(Path.GetTempPath(), "pohjoiseen-tests", Guid.NewGuid().ToString());

    private static readonly Lazy<string> Template = new(() =>
    {
        System.IO.Directory.CreateDirectory(Directory);
        var path = Path.Combine(Directory, "template.db");
        using (var db = Open(path))
        {
            db.Database.Migrate();
            TestData.Seed(db);
        }
        SqliteConnection.ClearAllPools();
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            SqliteConnection.ClearAllPools();
            try { System.IO.Directory.Delete(Directory, true); } catch (IOException) { }
        };
        return path;
    });

    public static string CreateCopy()
    {
        var path = Path.Combine(Directory, $"{Guid.NewGuid()}.db");
        File.Copy(Template.Value, path);
        return path;
    }

    /// <summary>
    /// Empty database file path (not created yet), for migration tests.
    /// </summary>
    public static string NewPath()
    {
        System.IO.Directory.CreateDirectory(Directory);
        return Path.Combine(Directory, $"{Guid.NewGuid()}.db");
    }

    public static HolviDbContext Open(string path) =>
        new(new DbContextOptionsBuilder<HolviDbContext>().UseSqlite($"Data Source={path}").Options);
}
