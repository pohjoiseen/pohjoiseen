using System.Net;
using Holvi.Models;
using Microsoft.EntityFrameworkCore;
using Tests.Infrastructure;
using static Tests.Infrastructure.TestData;

namespace Tests.KoTiTests;

/// <summary>
/// Posts, articles and books in KoTi, through HTTP like htmx would do it.  Editing in the browser is in Browser tests.
/// </summary>
public class ContentTests(KoTiFactory factory) : IClassFixture<KoTiFactory>
{
    private readonly HttpClient _client = factory.CreateNonRedirectingClient();
    private readonly HttpClient _htmx = factory.CreateHtmxClient();
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static FormUrlEncodedContent Form(params (string, string)[] fields) =>
        new(fields.Select(f => new KeyValuePair<string, string>(f.Item1, f.Item2)));

    [Fact]
    public async Task Home()
    {
        var doc = await _client.GetDocumentAsync("/");
        var text = doc.Body!.TextContent;
        Assert.Contains("3.5", text);  // version
        Assert.Contains("test", text);  // bucket
    }

    [Theory]
    [InlineData("Posts", "Остров 26", "postSearch", "турку", "Турку")]
    [InlineData("Articles", "О проекте", "articleSearch", "draft", "Черновик статьи")]
    [InlineData("Books", "Лапландия", "bookSearch", "archi", "Архипелаг")]
    public async Task ListPages(string kind, string title, string searchParam, string search, string found)
    {
        static List<string?> Titles(AngleSharp.Dom.IDocument doc) =>
            doc.QuerySelectorAll("koti-content-item").Select(e => e.GetAttribute("title")).ToList();

        var doc = await _client.GetDocumentAsync($"/{kind}/ru");
        Assert.Contains(title, Titles(doc));

        // search, updates list via htmx
        var list = await _htmx.GetDocumentAsync($"/{kind}/List/list/ru?{searchParam}={search}&limit=25&offset=0");
        Assert.Equal([found], Titles(list));
    }

    [Theory]
    [InlineData("Posts", HelsinkiId, "Хельсинки")]
    [InlineData("Articles", AboutId, "О проекте")]
    [InlineData("Books", LaplandBookId, "Лапландия")]
    public async Task EditPage(string kind, int id, string title)
    {
        // without language, redirects to Russian version
        var response = await _client.GetAsync($"/{kind}/{id}", Ct);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal($"/{kind}/{id}/ru/", response.Headers.Location!.ToString());

        var doc = await _client.GetDocumentAsync($"/{kind}/{id}/ru");
        Assert.Equal(title, doc.QuerySelector("#content-form input#title")!.GetAttribute("value"));
        Assert.NotNull(doc.QuerySelector("koti-content-editor"));
        Assert.NotNull(doc.QuerySelector("textarea#content-md-initial"));
    }

    [Fact]
    public async Task EditPostShowsAllLanguages()
    {
        var doc = await _client.GetDocumentAsync($"/Posts/{HelsinkiId}/ru");
        Assert.Equal($"/Posts/{HelsinkiEnId}/en/", doc.QuerySelector("koti-dropdown-option[href$='/en/']")!.GetAttribute("href"));
        Assert.Equal($"/Posts/{HelsinkiId}/ru/copyTo/fi/", doc.QuerySelector("koti-dropdown-option[muted]")!.GetAttribute("href"));
        Assert.Equal("Сенатская площадь", doc.QuerySelector("input[name='Geo[0][Title]']")!.GetAttribute("value"));
        Assert.NotNull(doc.QuerySelector($"input[name='CoatsOfArms[0][Url]'][value='picture:{CoatId}']"));
    }

    [Fact]
    public async Task CreateEditDeletePost()
    {
        // validation
        var response = await _htmx.PostAsync("/Posts/Create/ru", Form(("Name", ""), ("Title", "")), Ct);
        Assert.False(response.Headers.Contains("HX-Redirect"));

        // create, redirects to editing
        response = await _htmx.PostAsync("/Posts/Create/ru", Form(("Name", "new-post"), ("Title", "Новый пост")), Ct);
        var editUrl = response.Headers.GetValues("HX-Redirect").Single();
        Assert.Matches("^/Posts/[0-9]+/ru/$", editUrl);
        var id = Int32.Parse(editUrl.Split('/')[2]);
        await using (var db = factory.OpenDb())
        {
            var post = await db.Posts.SingleAsync(p => p.Id == id, Ct);
            Assert.Equal("new-post", post.Name);
            Assert.True(post.Draft);
            Assert.Equal(DateOnly.FromDateTime(DateTime.Now), post.Date);
            Assert.True(DateTime.UtcNow - post.UpdatedAt < TimeSpan.FromMinutes(1));
        }

        // save, like the editor would do it
        response = await _htmx.PutAsync(editUrl, Form(
            ("Name", "new-post"), ("Date", "2024-03-01"), ("Title", "Новый пост!"), ("Language", "ru"),
            ("Description", "Описание"), ("ContentMD", "## Текст\n\nПривет"), ("Draft", "false"), ("Mini", "true"),
            ("TitlePictureId", LandscapeId.ToString()), ("BookId", LaplandBookId.ToString()),
            ("CoatsOfArms[0][Url]", $"picture:{CoatId}"), ("CoatsOfArms[0][Size]", "100"),
            ("Geo[0][Lat]", "60.5"), ("Geo[0][Lng]", "25.5"), ("Geo[0][Maps][]", "index"), ("Geo[0][Maps][]", "osm"),
            ("Geo[0][Links][0][Path]", $"post:{TurkuId}"), ("Geo[0][Links][0][Label]", "Турку")), Ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        // title in header is updated out of band
        var saved = HttpClientExtensions.ParseHtml(await response.Content.ReadAsStringAsync(Ct));
        Assert.Contains("Новый пост!", saved.QuerySelector("#header-title")!.TextContent);

        await using (var db = factory.OpenDb())
        {
            var post = await db.Posts.SingleAsync(p => p.Id == id, Ct);
            Assert.Equal("Новый пост!", post.Title);
            Assert.Equal(new DateOnly(2024, 3, 1), post.Date);
            Assert.Equal("## Текст\n\nПривет", post.ContentMD);
            Assert.False(post.Draft);
            Assert.True(post.Mini);
            Assert.Equal(LandscapeId, post.TitlePictureId);
            Assert.Equal(LaplandBookId, post.BookId);
            Assert.Equal(new Post.CoatOfArms { Url = $"picture:{CoatId}", Size = 100 }, Assert.Single(post.CoatsOfArms!));
            var geo = Assert.Single(post.Geo!);
            Assert.Equal((60.5, 25.5), (geo.Lat, geo.Lng));
            Assert.Equal(["index", "osm"], geo.Maps!);
            Assert.Equal(new Post.Link { Path = $"post:{TurkuId}", Label = "Турку" }, Assert.Single(geo.Links!));
        }

        // now visible in preview, in the book
        var preview = await _client.GetDocumentAsync("/ru/lapland/new-post/");
        Assert.Equal("Новый пост!", preview.QuerySelector("h1")!.TextContent);
        Assert.Equal("текст", preview.QuerySelector("article h2")!.Id);

        // delete
        response = await _htmx.DeleteAsync($"/Posts/{id}/ru", Ct);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("/Posts/ru/", response.Headers.GetValues("HX-Redirect").Single());
        await using (var db = factory.OpenDb())
        {
            Assert.False(await db.Posts.AnyAsync(p => p.Id == id, Ct));
        }
    }

    [Fact]
    public async Task SaveWithValidationErrors()
    {
        var response = await _htmx.PutAsync($"/Posts/{TurkuId}/ru", Form(("Name", "turku"), ("Title", "Турку"),
            ("Date", "not a date"), ("Language", "ru")), Ct);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var doc = HttpClientExtensions.ParseHtml(await response.Content.ReadAsStringAsync(Ct));
        Assert.NotNull(doc.QuerySelector(".alert .validation-summary-errors"));
        await using var db = factory.OpenDb();
        Assert.Equal(new DateOnly(2020, 6, 15), (await db.Posts.SingleAsync(p => p.Id == TurkuId, Ct)).Date);
    }

    [Fact]
    public async Task CopyPostToLanguage()
    {
        // confirmation page first
        var doc = await _client.GetDocumentAsync($"/Posts/{TurkuId}/ru/copyTo/fi");
        Assert.NotNull(doc.QuerySelector("form"));

        var response = await _htmx.PostAsync($"/Posts/{TurkuId}/ru/copyTo/fi", null, Ct);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var id = Int32.Parse(response.Headers.Location!.ToString().Split('/')[2]);

        await using var db = factory.OpenDb();
        var copy = await db.Posts.SingleAsync(p => p.Id == id, Ct);
        Assert.Equal(("fi", "turku", new DateOnly(2020, 6, 15), true), (copy.Language, copy.Name, copy.Date, copy.Draft));
        Assert.Equal("Турку", copy.Title);
        Assert.Single(copy.Geo!);
    }

    [Theory]
    [InlineData("Articles")]
    [InlineData("Books")]
    public async Task CreateEditDeleteArticleOrBook(string kind)
    {
        var response = await _htmx.PostAsync($"/{kind}/Create/ru", Form(("Name", "new-thing"), ("Title", "Новое")), Ct);
        var editUrl = response.Headers.GetValues("HX-Redirect").Single();
        var id = Int32.Parse(editUrl.Split('/')[2]);

        response = await _htmx.PutAsync(editUrl, Form(("Name", "new-thing"), ("Title", "Новое!"), ("Language", "ru"),
            ("ContentMD", "Текст"), ("Draft", "true")), Ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using (var db = factory.OpenDb())
        {
            var saved = kind == "Articles"
                ? await db.Articles.Where(a => a.Id == id).Select(a => new { a.Title, a.ContentMD, a.Draft }).SingleAsync(Ct)
                : await db.Books.Where(b => b.Id == id).Select(b => new { b.Title, b.ContentMD, b.Draft }).SingleAsync(Ct);
            Assert.Equal(new { Title = "Новое!", ContentMD = "Текст", Draft = true }, saved);
        }

        response = await _htmx.DeleteAsync($"/{kind}/{id}/ru", Ct);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DraftsArePreviewed()
    {
        var doc = await _client.GetDocumentAsync(DraftPostPath);
        Assert.Equal("Черновик", doc.QuerySelector("h1")!.TextContent);
        // Fennica3 static files are served as well
        (await _client.GetAsync("/css/style.css", Ct)).EnsureSuccessStatusCode();
        (await _client.GetAsync("/logo.svg", Ct)).EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Api()
    {
        var post = await _client.GetStringAsync($"/api/Posts/{HelsinkiId}", Ct);
        Assert.Contains("\"name\":\"helsinki\"", post);
        var article = await _client.GetStringAsync($"/api/Articles/{AboutId}", Ct);
        Assert.Contains("\"name\":\"about\"", article);
        var picture = await _client.GetStringAsync($"/api/Pictures/{LandscapeId}", Ct);
        Assert.Contains("\"filename\":\"landscape.jpg\"", picture);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync("/api/Posts/9999", Ct)).StatusCode);
    }
}
