using Holvi.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Tests.Infrastructure;

namespace Tests;

public class DatabaseTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public void ModelHasNoChangesWithoutMigration()
    {
        using var db = TestDatabase.Open(TestDatabase.CreateCopy());
        Assert.False(db.Database.HasPendingModelChanges(), "Model changed, add a migration");
    }

    [Theory]
    [InlineData("Posts")]
    [InlineData("Articles")]
    [InlineData("Books")]
    [InlineData("PictureSets")]
    public async Task SearchTriggersExist(string table)
    {
        // EF silently drops triggers when it rebuilds a table in a migration, see SearchRebuild migration
        await using var db = TestDatabase.Open(TestDatabase.CreateCopy());
        var triggers = await db.Database
            .SqlQuery<string>($"SELECT name AS Value FROM sqlite_master WHERE type = 'trigger' AND tbl_name = {table}")
            .ToListAsync(Ct);
        Assert.Equal([$"{table}_SearchDelete", $"{table}_SearchInsert", $"{table}_SearchUpdate"], triggers.Order());
    }

    [Fact]
    public async Task UpdatedAtIsSetOnSave()
    {
        await using var db = TestDatabase.Open(TestDatabase.CreateCopy());
        var before = DateTime.UtcNow;
        var article = new Article { Name = "x", Language = "ru", Title = "X" };
        db.Articles.Add(article);
        await db.SaveChangesAsync(Ct);
        Assert.InRange(article.UpdatedAt, before, DateTime.UtcNow);

        var created = article.UpdatedAt;
        article.Title = "Y";
        await db.SaveChangesAsync(Ct);
        Assert.True(article.UpdatedAt >= created);
    }
}
