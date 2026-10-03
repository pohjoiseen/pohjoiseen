using System.Net;
using Microsoft.EntityFrameworkCore;
using Tests.Infrastructure;
using static Tests.Infrastructure.TestData;

namespace Tests.KoTiTests;

public class RedirectTests(KoTiFactory factory) : IClassFixture<KoTiFactory>
{
    private readonly HttpClient _htmx = factory.CreateHtmxClient();
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private Task<HttpResponseMessage> Add(string kind, string from, string to, string anchor = "") =>
        _htmx.PostAsync($"/Redirects/Add/{kind}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["urlFrom"] = from, ["urlTo"] = to, ["anchor"] = anchor
        }), Ct);

    [Fact]
    public async Task List()
    {
        var doc = await factory.CreateClient().GetDocumentAsync("/Redirects");
        var text = doc.Body!.TextContent;
        Assert.Contains("/old/helsinki", text);
        Assert.Contains("Хельсинки (2020-05-01)", text);  // resolved post title
        Assert.Contains("О проекте", text);  // resolved article title
        Assert.Contains("https://example.com/x", text);
        Assert.NotNull(doc.QuerySelector($"a[href='/Posts/{HelsinkiId}/ru/']"));
    }

    [Fact]
    public async Task AddAndDelete()
    {
        // full URLs are accepted, only path is kept; anchor is appended
        var response = await Add("post", "https://fennica.pohjoiseen.fi/some/old/путь/", $"post:{TurkuId}", "#anchor");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("/Redirects/", response.Headers.GetValues("HX-Redirect").Single());

        await using var db = factory.OpenDb();
        var redirect = await db.Redirects.SingleAsync(r => r.UrlFrom == "/some/old/путь/", Ct);
        Assert.Equal($"post:{TurkuId}#anchor", redirect.UrlTo);

        // delete returns updated list
        response = await _htmx.DeleteAsync($"/Redirects/{redirect.Id}", Ct);
        response.EnsureSuccessStatusCode();
        Assert.DoesNotContain("/some/old/", await response.Content.ReadAsStringAsync(Ct));
        Assert.False(await db.Redirects.AnyAsync(r => r.Id == redirect.Id, Ct));
    }

    [Theory]
    [InlineData("url", "old/path", "https://example.com/", "must be a path")]
    [InlineData("url", "/new/path", "", "Target URL is required")]
    [InlineData("post", "/new/path", "article:1", "Please select a post")]
    [InlineData("article", "/new/path", "post:1", "Please select an article")]
    [InlineData("post", "/new/path", "post:9999", "does not exist")]
    [InlineData("post", "/old/helsinki/", "post:2", "already exists")]  // same with a trailing slash
    [InlineData("url", "/old/about", "https://example.com/", "already exists")]  // same without
    public async Task Validation(string kind, string from, string to, string error)
    {
        var response = await Add(kind, from, to);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(error, await response.Content.ReadAsStringAsync(Ct));
    }
}
