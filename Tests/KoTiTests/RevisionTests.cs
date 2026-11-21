using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Holvi.Models;
using KoTi.ModelFactories;
using KoTi.Revisions;
using Microsoft.EntityFrameworkCore;
using Tests.Infrastructure;

namespace Tests.KoTiTests;

/// <summary>
/// History of content: which saves make a revision, thinning, API, edit page.
/// </summary>
public class RevisionTests(KoTiFactory factory) : IClassFixture<KoTiFactory>
{
    private readonly HttpClient _client = factory.CreateNonRedirectingClient();
    private readonly HttpClient _htmx = factory.CreateHtmxClient();
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private async Task<int> CreatePostAsync(string name, string content)
    {
        await using var db = factory.OpenDb();
        var post = new Post { Name = name, Title = name, Language = "ru", Date = new DateOnly(2019, 2, 1), Draft = true, ContentMD = content };
        db.Add(post);
        await db.SaveChangesAsync(Ct);
        return post.Id;
    }

    private async Task<List<Revision>> InDbAsync(int postId)
    {
        await using var db = factory.OpenDb();
        return await db.Revisions.Where(r => r.TargetType == "post" && r.TargetId == postId).OrderBy(r => r.Id).ToListAsync(Ct);
    }

    /// <summary>
    /// Save as the edit page does, at a given moment.
    /// </summary>
    private async Task SaveAsync(int postId, string content, DateTime now)
    {
        await using var db = factory.OpenDb();
        var post = await db.Posts.SingleAsync(p => p.Id == postId, Ct);
        var version = post.Version;
        post.ContentMD = content;
        await RevisionRecorder.RecordAsync(db, post, version + 1, now);
        post.Version = version + 1;
        await db.SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task SavingFromEditPageRecordsRevisions()
    {
        var id = await CreatePostAsync("revisions-save", "Исходный текст.");

        async Task<HttpStatusCode> Save(int version, string content, string title = "revisions-save") =>
            (await _htmx.PutAsync($"/Posts/{id}/ru", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Name"] = "revisions-save", ["Title"] = title, ["Date"] = "2019-02-01", ["Language"] = "ru",
                ["ContentMD"] = content, ["Version"] = version.ToString(),
            }), Ct)).StatusCode;

        // text not changed: nothing
        Assert.Equal(HttpStatusCode.OK, await Save(0, "Исходный текст.", "Другой заголовок"));
        Assert.Empty(await InDbAsync(id));

        // first change: the text before it is kept too
        Assert.Equal(HttpStatusCode.OK, await Save(1, "Новый текст."));
        var revisions = await InDbAsync(id);
        Assert.Equal(["Исходный текст.", "Новый текст."], revisions.Select(r => r.GetContent()));
        Assert.Equal([1, 2], revisions.Select(r => r.Version));
        Assert.Equal([true, false], revisions.Select(r => r.Keep));
        Assert.Equal("Новый текст.".Length, revisions[1].ContentLength);

        // saved again right away: goes into the same revision
        Assert.Equal(HttpStatusCode.OK, await Save(2, "Новый текст, дописанный."));
        revisions = await InDbAsync(id);
        Assert.Equal(["Исходный текст.", "Новый текст, дописанный."], revisions.Select(r => r.GetContent()));
        Assert.Equal(3, revisions[1].Version);

        // rejected save records nothing
        Assert.Equal(HttpStatusCode.Conflict, await Save(1, "Из устаревшей вкладки."));
        Assert.Equal(2, (await InDbAsync(id)).Count);

        // edit page and API
        var doc = await _client.GetDocumentAsync($"/Posts/{id}/ru");
        var rows = doc.QuerySelectorAll("koti-history tr[data-id]");
        Assert.Equal(revisions.Select(r => r.Id.ToString()).Reverse(), rows.Select(r => r.GetAttribute("data-id")));
        var list = await _client.GetFromJsonAsync<JsonElement>($"/api/Revisions?targetType=post&targetId={id}", Ct);
        Assert.Equal(2, list.GetArrayLength());
        Assert.False(list[0].TryGetProperty("contentMD", out _));
        var one = await _client.GetFromJsonAsync<JsonElement>($"/api/Revisions/{revisions[0].Id}", Ct);
        Assert.Equal("Исходный текст.", one.GetProperty("contentMD").GetString());
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync("/api/Revisions/99999", Ct)).StatusCode);

        // publishing: the current text stays in history, later saves do not overwrite it
        Assert.Equal(HttpStatusCode.OK, (await _client.PostAsync("/Publish", null, Ct)).StatusCode);
        Assert.Equal([true, true], (await InDbAsync(id)).Select(r => r.Keep));
        Assert.Equal(HttpStatusCode.OK, await Save(3, "После публикации."));
        Assert.Equal(["Исходный текст.", "Новый текст, дописанный.", "После публикации."], (await InDbAsync(id)).Select(r => r.GetContent()));

        // deleted together with the post
        Assert.Equal(HttpStatusCode.NoContent, (await _htmx.DeleteAsync($"/Posts/{id}/ru", Ct)).StatusCode);
        Assert.Empty(await InDbAsync(id));
    }

    [Fact]
    public async Task ManySavesMakeFewRevisions()
    {
        var id = await CreatePostAsync("revisions-thinning", "Текст 0");
        var start = new DateTime(2026, 1, 10, 9, 0, 0, DateTimeKind.Utc);

        // writing for three days, four hours a day, saving every five minutes
        var n = 0;
        for (var day = 0; day < 3; day++)
        {
            for (var minute = 0; minute < 240; minute += 5)
            {
                await SaveAsync(id, $"Текст {++n}", start.AddDays(day).AddMinutes(minute));
            }
        }

        // one per hour of work, plus the original
        var revisions = await InDbAsync(id);
        Assert.Equal(1 + 3 * 4, revisions.Count);
        Assert.Equal("Текст 0", revisions[0].GetContent());
        Assert.Equal($"Текст {n}", revisions[^1].GetContent());
        // each has the last text saved within its hour
        Assert.Equal("Текст 12", revisions[1].GetContent());
        Assert.Equal(start.AddMinutes(55), revisions[1].SavedAt);

        // a published one in between is never touched
        await using (var db = factory.OpenDb())
        {
            var kept = await db.Revisions.SingleAsync(r => r.Id == revisions[2].Id, Ct);
            kept.Keep = true;
            await db.SaveChangesAsync(Ct);
        }

        // two months later: of the old ones only the last of each day is left, and what is kept
        await SaveAsync(id, "Через два месяца", start.AddDays(60));
        var thinned = await InDbAsync(id);
        Assert.Equal(["Текст 0", "Текст 24", "Текст 48", "Текст 96", "Текст 144", "Через два месяца"], thinned.Select(r => r.GetContent()));
    }

    [Fact]
    public async Task ArticlesAndBooksHaveHistoryToo()
    {
        await using (var db = factory.OpenDb())
        {
            var article = await db.Articles.SingleAsync(a => a.Id == TestData.BlogIntroId, Ct);
            var before = article.ContentMD;
            article.ContentMD = before + "\n\nДобавлено.";
            await db.SaveVersionedAsync(article, article.Version);
            var book = await db.Books.SingleAsync(b => b.Id == TestData.DraftBookId, Ct);
            book.ContentMD += "\n\nДобавлено.";
            await db.SaveVersionedAsync(book, book.Version);

            var revisions = await db.Revisions.Where(r => r.TargetType == "article" && r.TargetId == TestData.BlogIntroId).OrderBy(r => r.Id).ToListAsync(Ct);
            Assert.Equal([before, before + "\n\nДобавлено."], revisions.Select(r => r.GetContent()));
            Assert.Equal(2, await db.Revisions.CountAsync(r => r.TargetType == "book" && r.TargetId == TestData.DraftBookId, Ct));
        }

        var doc = await _client.GetDocumentAsync($"/Articles/{TestData.BlogIntroId}/ru");
        Assert.Equal(2, doc.QuerySelectorAll("koti-history tr[data-id]").Length);
    }
}
