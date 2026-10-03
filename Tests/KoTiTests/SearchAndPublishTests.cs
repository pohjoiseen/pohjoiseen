using System.Net;
using Tests.Infrastructure;
using static Tests.Infrastructure.TestData;

namespace Tests.KoTiTests;

public class SearchTests(KoTiFactory factory) : IClassFixture<KoTiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private async Task<List<string>> Search(string q)
    {
        var doc = await _client.GetDocumentAsync($"/Search?q={Uri.EscapeDataString(q)}");
        Assert.Null(doc.QuerySelector(".error"));
        return doc.QuerySelectorAll("ol a").Select(a => a.GetAttribute("href")!).ToList();
    }

    [Fact]
    public async Task FindsPostsArticlesBooks()
    {
        Assert.Contains($"/Posts/{HelsinkiId}/", await Search("столица"));
        Assert.Contains($"/Articles/{AboutId}/", await Search("автор"));
        Assert.Contains($"/Books/{LaplandBookId}/", await Search("лапландии"));
    }

    [Fact]
    public async Task IgnoresCase()
    {
        // "её" in Helsinki description
        Assert.Contains($"/Posts/{HelsinkiId}/", await Search("ЕЁ"));
        // NB: diacritics are removed only from Latin letters, ё is not е
        Assert.DoesNotContain($"/Posts/{HelsinkiId}/", await Search("ее"));
    }

    [Fact]
    public async Task IndexIsUpdatedOnChanges()
    {
        // full-text index is maintained by triggers
        var htmx = factory.CreateHtmxClient();
        var response = await htmx.PostAsync("/Articles/Create/ru", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Name"] = "searchable", ["Title"] = "Уникальнейшее название"
        }), Ct);
        var id = Int32.Parse(response.Headers.GetValues("HX-Redirect").Single().Split('/')[2]);
        Assert.Equal([$"/Articles/{id}/"], await Search("уникальнейшее"));

        await htmx.DeleteAsync($"/Articles/{id}/ru", Ct);
        Assert.Empty(await Search("уникальнейшее"));
    }

    [Fact]
    public async Task InvalidQueryShowsError()
    {
        var doc = await _client.GetDocumentAsync("/Search?q=%22unterminated");
        Assert.NotNull(doc.QuerySelector(".error"));
    }
}

public class PublishTests(KoTiFactory factory) : IClassFixture<KoTiFactory>
{
    [Fact]
    public async Task Publish()
    {
        var client = factory.CreateClient();
        await client.GetDocumentAsync("/Publish");

        // make a change, then publish
        await using (var db = factory.OpenDb())
        {
            db.Posts.Find(TurkuId)!.Title = "Опубликованный Турку";
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
        var response = await client.PostAsync("/Publish", null, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var doc = HttpClientExtensions.ParseHtml(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Equal("published", doc.QuerySelector("pre")!.TextContent.Trim());

        // publish command (cp in tests) has copied the database
        await using var live = TestDatabase.Open(factory.LiveDatabasePath);
        Assert.Equal("Опубликованный Турку", live.Posts.Find(TurkuId)!.Title);
    }
}
