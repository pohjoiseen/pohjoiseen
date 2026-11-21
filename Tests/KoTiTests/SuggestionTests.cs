using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Holvi.Models;
using Microsoft.EntityFrameworkCore;
using Tests.Infrastructure;
using static Tests.Infrastructure.TestData;

namespace Tests.KoTiTests;

/// <summary>
/// Suggested edits to content: API which creates them, edit page which shows them, accepting on save.
/// What happens in the editor is in Browser tests.
/// </summary>
public class SuggestionTests(KoTiFactory factory) : IClassFixture<KoTiFactory>
{
    private readonly HttpClient _client = factory.CreateNonRedirectingClient();
    private readonly HttpClient _htmx = factory.CreateHtmxClient();
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private const string Content = "Первый абзац с ашипкой.\n\nВторой абзац, поезда ходят.\n\nПовтор. Повтор.";

    private async Task<int> CreatePostAsync(string name, string content = Content)
    {
        await using var db = factory.OpenDb();
        var post = new Post { Name = name, Title = name, Language = "ru", Date = new DateOnly(2019, 1, 1), Draft = true, ContentMD = content };
        db.Add(post);
        await db.SaveChangesAsync(Ct);
        return post.Id;
    }

    private static object Suggestion(string oldText, string newText, string kind = "correction", string? comment = null, string[]? sources = null) =>
        new { kind, oldText, newText, comment, sources };

    private async Task<JsonElement> CreateAsync(int postId, string batch, params object[] suggestions)
    {
        var response = await _client.PostAsJsonAsync("/api/Suggestions",
            new { targetType = "post", targetId = postId, batch, suggestions }, Ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
    }

    private async Task<List<Suggestion>> InDbAsync(int postId)
    {
        await using var db = factory.OpenDb();
        return await db.Suggestions.Where(s => s.TargetType == "post" && s.TargetId == postId).OrderBy(s => s.Id).ToListAsync(Ct);
    }

    [Fact]
    public async Task CreateChecksSuggestionsAgainstContent()
    {
        // line endings in the database may be either
        var id = await CreatePostAsync("suggestions-create", Content.Replace("\n", "\r\n"));

        var result = await CreateAsync(id, "test-create",
            Suggestion("ашипкой", "ошибкой", comment: " Опечатка "),
            Suggestion("нет такого текста", "что-то"),
            Suggestion("Повтор.", "Один раз."),
            Suggestion("абзац", "абзац"),
            Suggestion("", "вставка"),
            Suggestion("Второй", "Третий", kind: "rewrite"),
            Suggestion("ашипкой", "ошибкой"),
            Suggestion("ашипкой.\n\nВторой абзац, поезда ходят.", "ашипкой.\n\nВторой абзац, поезда ходят. **Обновление 2026**: уже не ходят.",
                kind: "update", sources: ["https://example.com/news", " "]));

        Assert.Equal(2, result.GetProperty("created").GetArrayLength());
        var rejected = result.GetProperty("rejected").EnumerateArray()
            .ToDictionary(r => r.GetProperty("index").GetInt32(), r => r.GetProperty("error").GetString()!);
        Assert.Equal([1, 2, 3, 4, 5, 6], rejected.Keys.Order());
        Assert.Contains("not found", rejected[1]);
        Assert.Contains("occurs 2 times", rejected[2]);
        Assert.Contains("the same", rejected[3]);
        Assert.Contains("empty", rejected[4]);
        Assert.Contains("kind must be", rejected[5]);
        Assert.Contains("already pending", rejected[6]);

        var saved = await InDbAsync(id);
        Assert.Equal(2, saved.Count);
        Assert.All(saved, s => Assert.Equal((SuggestionStatus.Pending, "test-create"), (s.Status, s.Batch)));
        Assert.Equal(("correction", "ашипкой", "ошибкой", "Опечатка", ""), (saved[0].Kind, saved[0].OldText, saved[0].NewText, saved[0].Comment, saved[0].Sources));
        Assert.Equal(("update", "https://example.com/news"), (saved[1].Kind, saved[1].Sources));
        Assert.True(DateTime.UtcNow - saved[0].CreatedAt < TimeSpan.FromMinutes(1));

        // content is not touched by any of this
        await using (var db = factory.OpenDb())
        {
            Assert.Equal(Content.Replace("\n", "\r\n"), (await db.Posts.SingleAsync(p => p.Id == id, Ct)).ContentMD);
        }

        // listing
        var list = await _client.GetFromJsonAsync<JsonElement>($"/api/Suggestions?targetType=post&targetId={id}&status=pending", Ct);
        Assert.Equal(2, list.GetArrayLength());
        Assert.Equal("ошибкой", list[0].GetProperty("newText").GetString());
        Assert.Equal("pending", list[0].GetProperty("status").GetString());
        Assert.Equal("https://example.com/news", list[1].GetProperty("sources")[0].GetString());
        Assert.Equal(2, (await _client.GetFromJsonAsync<JsonElement>("/api/Suggestions?batch=test-create", Ct)).GetArrayLength());
        Assert.Equal(0, (await _client.GetFromJsonAsync<JsonElement>($"/api/Suggestions?targetId={id}&status=accepted", Ct)).GetArrayLength());
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.GetAsync("/api/Suggestions?status=unknown", Ct)).StatusCode);
    }

    [Fact]
    public async Task CreateRequiresTargetAndBatch()
    {
        var id = await CreatePostAsync("suggestions-invalid");
        object[] suggestions = [Suggestion("ашипкой", "ошибкой")];

        async Task<HttpStatusCode> Post(object body) => (await _client.PostAsJsonAsync("/api/Suggestions", body, Ct)).StatusCode;

        Assert.Equal(HttpStatusCode.BadRequest, await Post(new { targetType = "picture", targetId = id, batch = "b", suggestions }));
        Assert.Equal(HttpStatusCode.BadRequest, await Post(new { targetType = "post", targetId = id, batch = " ", suggestions }));
        Assert.Equal(HttpStatusCode.NotFound, await Post(new { targetType = "post", targetId = 99999, batch = "b", suggestions }));
        Assert.Equal(HttpStatusCode.NotFound, await Post(new { targetType = "article", targetId = 99999, batch = "b", suggestions }));
        Assert.Empty(await InDbAsync(id));

        // articles and books work the same
        var response = await _client.PostAsJsonAsync("/api/Suggestions", new
        {
            targetType = "book", targetId = LaplandBookId, batch = "test-book",
            suggestions = new[] { Suggestion("нет такого текста в книге", "что-то") }
        }, Ct);
        Assert.Equal(1, (await response.Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("rejected").GetArrayLength());
    }

    [Fact]
    public async Task DeclineRestoreAndWithdrawBatch()
    {
        var id = await CreatePostAsync("suggestions-decline");
        var created = (await CreateAsync(id, "test-decline",
            Suggestion("ашипкой", "ошибкой"), Suggestion("поезда ходят", "поезда не ходят"))).GetProperty("created");
        var (first, second) = (created[0].GetProperty("id").GetInt32(), created[1].GetProperty("id").GetInt32());

        async Task<HttpResponseMessage> SetStatus(int suggestionId, string status) =>
            await _client.PutAsJsonAsync($"/api/Suggestions/{suggestionId}/status", new { status }, Ct);

        Assert.Equal(HttpStatusCode.OK, (await SetStatus(first, "declined")).StatusCode);
        var saved = await InDbAsync(id);
        Assert.Equal([SuggestionStatus.Declined, SuggestionStatus.Pending], saved.Select(s => s.Status));
        Assert.NotNull(saved[0].ResolvedAt);

        Assert.Equal(HttpStatusCode.OK, (await SetStatus(first, "pending")).StatusCode);
        saved = await InDbAsync(id);
        Assert.Equal(SuggestionStatus.Pending, saved[0].Status);
        Assert.Null(saved[0].ResolvedAt);

        // accepting is not possible through the API
        Assert.Equal(HttpStatusCode.BadRequest, (await SetStatus(first, "accepted")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await SetStatus(99999, "declined")).StatusCode);

        // withdrawing a batch removes only what is still pending
        Assert.Equal(HttpStatusCode.OK, (await SetStatus(second, "declined")).StatusCode);
        var response = await _client.DeleteAsync("/api/Suggestions?batch=test-decline", Ct);
        Assert.Equal("1", await response.Content.ReadAsStringAsync(Ct));
        Assert.Equal([second], (await InDbAsync(id)).Select(s => s.Id));
        Assert.Equal(HttpStatusCode.BadRequest, (await _client.DeleteAsync("/api/Suggestions", Ct)).StatusCode);
    }

    [Fact]
    public async Task EditPageShowsPendingAndSavingAccepts()
    {
        var id = await CreatePostAsync("suggestions-edit");
        var otherId = await CreatePostAsync("suggestions-edit-other");
        var created = (await CreateAsync(id, "test-edit",
            Suggestion("ашипкой.\n\nВторой", "ошибкой.\n\nВторой", comment: "Опечатка <b>", sources: ["https://example.com/a?b=1&c=2", "javascript:alert(1)"]),
            Suggestion("поезда ходят", "поезда не ходят", kind: "update"),
            Suggestion("Первый", "Самый первый"))).GetProperty("created");
        var ids = created.EnumerateArray().Select(c => c.GetProperty("id").GetInt32()).ToList();
        var otherSuggestionId = (await CreateAsync(otherId, "test-edit", Suggestion("ашипкой", "ошибкой")))
            .GetProperty("created")[0].GetProperty("id").GetInt32();
        Assert.Equal(HttpStatusCode.OK, (await _client.PutAsJsonAsync($"/api/Suggestions/{ids[2]}/status", new { status = "declined" }, Ct)).StatusCode);

        // pending ones are on the edit page, with exact texts for the editor
        var doc = await _client.GetDocumentAsync($"/Posts/{id}/ru");
        Assert.Equal("Suggestions (2)", doc.QuerySelector("koti-tab[name=Suggestions]")!.GetAttribute("label"));
        var cards = doc.QuerySelectorAll("koti-suggestions .suggestion");
        Assert.Equal(ids.Take(2).Select(i => i.ToString()), cards.Select(c => c.GetAttribute("data-id")));
        Assert.Equal("ашипкой.\n\nВторой", cards[0].GetAttribute("data-old"));
        Assert.Equal("ошибкой.\n\nВторой", cards[0].GetAttribute("data-new"));
        Assert.Equal("Опечатка <b>", cards[0].QuerySelector(".suggestion-comment")!.TextContent);
        Assert.Equal("update", cards[1].QuerySelector(".suggestion-kind")!.TextContent);
        // only web links are links
        Assert.Equal(["https://example.com/a?b=1&c=2"], cards[0].QuerySelectorAll(".suggestion-sources a").Select(a => a.GetAttribute("href")));
        Assert.Contains("javascript:alert(1)", cards[0].QuerySelector(".suggestion-sources")!.TextContent);
        Assert.Equal($"/Posts/{otherId}/", doc.QuerySelector("#next-with-suggestions-link")!.GetAttribute("href"));

        var fields = new Dictionary<string, string>
        {
            ["Name"] = "suggestions-edit", ["Title"] = "suggestions-edit", ["Date"] = "2019-01-01", ["Language"] = "ru",
            ["ContentMD"] = Content.Replace("ашипкой", "ошибкой"),
        };
        IEnumerable<KeyValuePair<string, string>> Form(string version, params int[] accepted) =>
            fields.Append(new("Version", version)).Concat(accepted.Select(a => new KeyValuePair<string, string>("AcceptedSuggestionIds", a.ToString())));

        // rejected save (outdated version) does not accept anything
        var response = await _htmx.PutAsync($"/Posts/{id}/ru", new FormUrlEncodedContent(Form("5", ids[0])), Ct);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.All((await InDbAsync(id)).Take(2), s => Assert.Equal(SuggestionStatus.Pending, s.Status));

        // saving accepts what was applied in the editor; only pending suggestions of this post
        response = await _htmx.PutAsync($"/Posts/{id}/ru", new FormUrlEncodedContent(Form("0", ids[0], ids[2], otherSuggestionId)), Ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var saved = await InDbAsync(id);
        Assert.Equal([SuggestionStatus.Accepted, SuggestionStatus.Pending, SuggestionStatus.Declined], saved.Select(s => s.Status));
        Assert.NotNull(saved[0].ResolvedAt);
        Assert.Equal(SuggestionStatus.Pending, (await InDbAsync(otherId)).Single().Status);
        Assert.Equal(HttpStatusCode.Conflict, (await _client.PutAsJsonAsync($"/api/Suggestions/{ids[0]}/status", new { status = "declined" }, Ct)).StatusCode);

        doc = await _client.GetDocumentAsync($"/Posts/{id}/ru");
        Assert.Equal("Suggestions (1)", doc.QuerySelector("koti-tab[name=Suggestions]")!.GetAttribute("label"));
        Assert.Equal(ids[1].ToString(), doc.QuerySelector("koti-suggestions .suggestion")!.GetAttribute("data-id"));

        // deleted together with the post
        Assert.Equal(HttpStatusCode.NoContent, (await _htmx.DeleteAsync($"/Posts/{id}/ru", Ct)).StatusCode);
        Assert.Empty(await InDbAsync(id));
        Assert.Single(await InDbAsync(otherId));
    }

    [Fact]
    public async Task PendingSuggestionsInListsAndOverview()
    {
        // nothing to show without suggestions
        var doc = await _client.GetDocumentAsync($"/Articles/{BlogIntroId}/ru");
        Assert.Equal("Suggestions", doc.QuerySelector("koti-tab[name=Suggestions]")!.GetAttribute("label"));
        Assert.Empty(doc.QuerySelectorAll("koti-suggestions .suggestion"));

        int articleId;
        await using (var db = factory.OpenDb())
        {
            var article = new Article { Name = "suggestions-article", Title = "Статья с предложениями", Language = "ru", ContentMD = Content };
            db.Add(article);
            await db.SaveChangesAsync(Ct);
            articleId = article.Id;
        }
        var response = await _client.PostAsJsonAsync("/api/Suggestions", new
        {
            targetType = "article", targetId = articleId, batch = "test-lists",
            suggestions = new[] { Suggestion("ашипкой", "ошибкой"), Suggestion("поезда ходят", "поезда не ходят") }
        }, Ct);
        Assert.Equal(2, (await response.Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("created").GetArrayLength());

        // article list and its API
        doc = await _client.GetDocumentAsync("/Articles/ru");
        Assert.Equal("2", doc.QuerySelector($"koti-content-item[id='{articleId}']")!.GetAttribute("suggestions"));
        Assert.Null(doc.QuerySelector($"koti-content-item[id='{BlogIntroId}']")!.GetAttribute("suggestions"));
        var articles = await _client.GetFromJsonAsync<JsonElement>("/api/Articles?language=ru", Ct);
        var about = articles.EnumerateArray().Single(a => a.GetProperty("id").GetInt32() == articleId);
        Assert.Equal(2, about.GetProperty("pendingSuggestions").GetInt32());
        Assert.Equal(Content.Length, about.GetProperty("contentLength").GetInt32());
        Assert.False(about.TryGetProperty("contentMD", out _));

        // overview and home page
        doc = await _client.GetDocumentAsync("/Suggestions");
        var row = doc.QuerySelectorAll("#suggestions-index tbody tr").Single(r => r.QuerySelector("a")!.GetAttribute("href") == $"/Articles/{articleId}/ru/");
        Assert.Contains("Статья с предложениями", row.TextContent);
        Assert.Contains("test-lists", row.TextContent);
        Assert.Equal("2", row.QuerySelector(".suggestions-badge")!.TextContent.Trim());
        doc = await _client.GetDocumentAsync("/");
        Assert.Matches(@"Review \([1-9]", doc.QuerySelector("#suggestions-link")!.TextContent);
    }

    [Fact]
    public async Task ContentListApi()
    {
        var posts = await _client.GetFromJsonAsync<JsonElement>("/api/Posts?language=ru", Ct);
        var helsinki = posts.EnumerateArray().Single(p => p.GetProperty("id").GetInt32() == HelsinkiId);
        Assert.Equal(("helsinki", "Хельсинки", "ru", false),
            (helsinki.GetProperty("name").GetString(), helsinki.GetProperty("title").GetString(),
                helsinki.GetProperty("language").GetString(), helsinki.GetProperty("draft").GetBoolean()));
        Assert.StartsWith("2020-05-01", helsinki.GetProperty("date").GetString());
        Assert.DoesNotContain(posts.EnumerateArray(), p => p.GetProperty("id").GetInt32() == HelsinkiEnId);
        Assert.Contains((await _client.GetFromJsonAsync<JsonElement>("/api/Posts", Ct)).EnumerateArray(),
            p => p.GetProperty("id").GetInt32() == HelsinkiEnId);

        var books = await _client.GetFromJsonAsync<JsonElement>("/api/Books?language=ru", Ct);
        Assert.Contains(books.EnumerateArray(), b => b.GetProperty("id").GetInt32() == LaplandBookId);
        var book = await _client.GetFromJsonAsync<JsonElement>($"/api/Books/{LaplandBookId}", Ct);
        Assert.Equal("Лапландия", book.GetProperty("title").GetString());
        Assert.Equal([InariId, KilpisjarviId], book.GetProperty("postIds").EnumerateArray().Select(p => p.GetInt32()));
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync("/api/Books/9999", Ct)).StatusCode);
    }
}
