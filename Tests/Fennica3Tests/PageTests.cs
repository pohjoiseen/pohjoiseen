using System.Net;
using System.Text.Json;
using System.Xml.Linq;
using Tests.Infrastructure;
using static Tests.Infrastructure.TestData;

namespace Tests.Fennica3Tests;

/// <summary>
/// Public blog pages, rendered fully through HTTP.
/// </summary>
public class PageTests(Fennica3Factory factory) : IClassFixture<Fennica3Factory>
{
    private readonly HttpClient _client = factory.CreateClient();
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task RootRedirectsToRussianBlog()
    {
        var response = await factory.CreateNonRedirectingClient().GetAsync("/", Ct);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/ru/", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task BlogFrontPage()
    {
        var doc = await _client.GetDocumentAsync("/ru/");

        Assert.Equal("Блог - Encyclopaedia Fennica", doc.Title);
        Assert.Equal("https://fennica.pohjoiseen.fi/ru/", doc.QuerySelector("link[rel=canonical]")!.GetAttribute("href"));
        var rss = await _client.GetAsync(doc.QuerySelector("link[type='application/rss+xml']")!.GetAttribute("href"), Ct);
        Assert.Equal("application/rss+xml", rss.Content.Headers.ContentType!.MediaType);

        // intro article, rendered
        Assert.Contains("Добро пожаловать в блог", doc.QuerySelector(".article-main")!.TextContent);

        // newest posts first, a page full; no drafts, no posts in books
        var titles = doc.QuerySelectorAll(".post-list-entry-title h2 a").Select(a => a.TextContent).ToList();
        Assert.Equal(24, titles.Count);
        Assert.Equal(["Турку", "Хельсинки", "Заполнитель 30"], titles.Take(3));
        Assert.DoesNotContain("Черновик", titles);
        Assert.DoesNotContain("Инари", titles);

        // post entry: title picture (website size), coats of arms, description as formatted Markdown
        var helsinki = doc.QuerySelectorAll("article.post-list-entry")[1];
        Assert.Equal(HelsinkiPath, helsinki.QuerySelector("h2 a")!.GetAttribute("href"));
        Assert.Equal(Picture(LandscapeId, "landscape.jpg", 3000, 2000, true).Website1xUrl,
            helsinki.QuerySelector(".post-list-entry-titleimage img")!.GetAttribute("src"));
        Assert.Equal($"{Url}hash{CoatId}/coat.png", helsinki.QuerySelector(".post-list-entry-coatsofarms img")!.GetAttribute("src"));
        Assert.Equal("Финляндии", helsinki.QuerySelector(".post-list-entry-description strong")!.TextContent);
        Assert.Equal("2020-05-01", helsinki.QuerySelector("time")!.GetAttribute("datetime"));

        // pagination
        Assert.Equal("1", doc.QuerySelector(".pagination .current")!.TextContent);
        Assert.Equal("/ru/2/", doc.QuerySelector(".pagination a.next")!.GetAttribute("href"));
    }

    [Fact]
    public async Task BlogFrontPageMaps()
    {
        var doc = await _client.GetDocumentAsync("/ru/");
        var wrapper = doc.QuerySelector(".mapview-wrapper")!;
        Assert.Equal("index,osm", wrapper.GetAttribute("data-maps"));

        // GeoJSON for 16 zoom levels; Helsinki point is on zoom level 3, Turku on 0, draft is not there
        var index = JsonDocument.Parse(wrapper.QuerySelector("[data-map=index]")!.GetAttribute("data-geojson")!).RootElement;
        Assert.Equal(16, index.GetArrayLength());
        var helsinki = Assert.Single(index[3].GetProperty("features").EnumerateArray());
        Assert.Equal("Сенатская площадь", helsinki.GetProperty("properties").GetProperty("title").GetString());
        Assert.Equal(HelsinkiPath, helsinki.GetProperty("properties").GetProperty("url").GetString());
        Assert.Equal("church", helsinki.GetProperty("properties").GetProperty("icon").GetString());
        Assert.Equal([24.9525, 60.1695], helsinki.GetProperty("geometry").GetProperty("coordinates").EnumerateArray().Select(c => c.GetDouble()));
        var turku = Assert.Single(index[0].GetProperty("features").EnumerateArray());
        Assert.Equal("Турку", turku.GetProperty("properties").GetProperty("title").GetString());

        // second map is hidden at first
        Assert.Equal("display: none;", wrapper.QuerySelector("[data-map=osm]")!.GetAttribute("style"));
    }

    [Fact]
    public async Task BlogSecondPage()
    {
        var doc = await _client.GetDocumentAsync("/ru/2/");
        var titles = doc.QuerySelectorAll(".post-list-entry-title h2 a").Select(a => a.TextContent).ToList();
        // 32 published posts outside of books in total
        Assert.Equal(8, titles.Count);
        Assert.Equal("Заполнитель 1", titles.Last());
        Assert.Null(doc.QuerySelector(".mapview-wrapper"));
        // first page link is without number
        Assert.Equal("/ru/", doc.QuerySelector(".pagination a.prev")!.GetAttribute("href"));
        Assert.Equal(["/ru/"], doc.QuerySelectorAll(".pagination a.page-numbers:not(.prev)").Select(a => a.GetAttribute("href")));
        Assert.Null(doc.QuerySelector(".pagination a.next"));
    }

    [Theory]
    [InlineData("/ru/3/")]                    // past the last page
    [InlineData("/fi/")]                      // no posts in this language
    [InlineData("/ru/2020/05/02/helsinki/")]  // wrong date
    [InlineData("/ru/2020/02/31/helsinki/")]  // invalid date
    [InlineData(DraftPostPath)]
    [InlineData("/ru/no-such-book/")]
    [InlineData("/ru/secret-book/")]          // draft book
    [InlineData("/ru/lapland/no-such-post/")]
    [InlineData("/ru/article/no-such-article/")]
    [InlineData("/ru/article/draft-article/")]
    [InlineData("/ru/article/_blog-intro/")]  // internal article
    public async Task NotFound(string url)
    {
        var response = await _client.GetAsync(url, Ct);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        // status code page, in our layout
        var doc = HttpClientExtensions.ParseHtml(await response.Content.ReadAsStringAsync(Ct));
        Assert.NotNull(doc.QuerySelector("header a.main-title"));
    }

    [Fact]
    public async Task Post()
    {
        var doc = await _client.GetDocumentAsync(HelsinkiPath);

        // head
        Assert.Equal("Хельсинки - Encyclopaedia Fennica", doc.Title);
        Assert.Equal("Столица Финляндии и её крупнейший город.", doc.QuerySelector("meta[name=description]")!.GetAttribute("content"));
        Assert.Equal("https://fennica.pohjoiseen.fi" + HelsinkiPath, doc.QuerySelector("link[rel=canonical]")!.GetAttribute("href"));
        Assert.Equal("article", doc.QuerySelector("meta[property='og:type']")!.GetAttribute("content"));
        Assert.Equal("2020-05-01", doc.QuerySelector("meta[property='article:published_time']")!.GetAttribute("content"));
        Assert.Equal($"{Url}hash{LandscapeId}/landscape.jpg", doc.QuerySelector("meta[property='og:image']")!.GetAttribute("content"));

        // header: title picture as background, coat of arms, descriptions, prev/next
        var heading = doc.QuerySelector(".post-heading")!;
        Assert.Contains($"url({Url}hash{LandscapeId}/landscape.jpg)", heading.GetAttribute("style"));
        Assert.Equal("Хельсинки", heading.QuerySelector("h1")!.TextContent);
        Assert.Equal($"{Url}hash{CoatId}/coat.png", heading.QuerySelector(".post-heading-coatsofarms img")!.GetAttribute("src"));
        Assert.Equal("Фото 2019 года", heading.QuerySelector(".time")!.TextContent);
        Assert.Equal("Южная Финляндия", heading.QuerySelector(".place")!.TextContent);
        Assert.Equal("/ru/2019/01/30/filler-30/", heading.QuerySelector(".prev a")!.GetAttribute("href"));
        Assert.Equal(TurkuPath, heading.QuerySelector(".next a")!.GetAttribute("href"));

        // content, formatted
        var article = doc.QuerySelector("main.post-main article")!;
        Assert.Equal("история-города", article.QuerySelector("h2")!.Id);
        Assert.NotNull(article.QuerySelector($"a[href='{TurkuPath}']"));
        Assert.NotNull(article.QuerySelector("figure img[srcset]"));
        Assert.NotNull(article.QuerySelector(".multiple-images-2"));
        Assert.NotNull(article.QuerySelector(".aside-wrapper aside"));

        // notes
        var notes = doc.QuerySelector(".notes")!.TextContent;
        Assert.Contains("Pohjoisesplanadi 11", notes);
        Assert.Contains("Трамвай 2", notes);
        Assert.Equal("2020-05-01", doc.QuerySelector(".notes time")!.GetAttribute("datetime"));

        // footer: prev/next
        Assert.Equal(2, doc.QuerySelectorAll($"footer a[href='{TurkuPath}']").Length);
    }

    [Fact]
    public async Task PostWithoutTitlePicture()
    {
        var doc = await _client.GetDocumentAsync(TurkuPath);
        Assert.NotNull(doc.QuerySelector(".post-heading-no-pic"));
        Assert.Null(doc.QuerySelector("meta[property='og:image']"));
        // newest post: no next
        Assert.Null(doc.QuerySelector(".post-heading .next"));
        Assert.Equal(HelsinkiPath + "#история-города", Uri.UnescapeDataString(doc.QuerySelector("article a")!.GetAttribute("href")!));
    }

    [Fact]
    public async Task PostInBook()
    {
        var doc = await _client.GetDocumentAsync(InariPath);
        Assert.Equal("Инари", doc.QuerySelector("h1")!.TextContent);
        Assert.Equal("/ru/lapland/", doc.QuerySelector(".post-title h2 a")!.GetAttribute("href"));
        // prev/next by order within book, not by date
        Assert.Null(doc.QuerySelector(".post-heading-no-pic .prev"));
        Assert.Equal(KilpisjarviPath, doc.QuerySelector(".post-heading-no-pic .next a")!.GetAttribute("href"));
        // footer has link up to book
        Assert.Equal("Лапландия", doc.QuerySelector("footer a.text[href='/ru/lapland/']")!.TextContent);
    }

    [Fact]
    public async Task PostInBookByDateRedirectsToBookUrl()
    {
        var response = await factory.CreateNonRedirectingClient().GetAsync("/ru/2021/01/10/inari/", Ct);
        Assert.Equal(HttpStatusCode.MovedPermanently, response.StatusCode);
        Assert.Equal(InariPath, response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Book()
    {
        var doc = await _client.GetDocumentAsync("/ru/lapland/");
        Assert.Equal("Лапландия", doc.QuerySelector(".post-heading-blog h1")!.TextContent);
        Assert.Equal("Лапландии", doc.QuerySelector(".article-main strong")!.TextContent);
        // ordered by order within book; no dates shown
        Assert.Equal(["Инари", "Килписъярви"], doc.QuerySelectorAll(".post-list-entry-title h2 a").Select(a => a.TextContent));
        Assert.Empty(doc.QuerySelectorAll(".post-list-entry-title time"));
        Assert.Null(doc.QuerySelector(".pagination"));
    }

    [Fact]
    public async Task BookPagination()
    {
        var doc = await _client.GetDocumentAsync("/ru/archipelago/");
        Assert.Equal(24, doc.QuerySelectorAll(".post-list-entry").Length);
        Assert.Equal("/ru/archipelago/2/", doc.QuerySelector(".pagination a.next")!.GetAttribute("href"));

        doc = await _client.GetDocumentAsync("/ru/archipelago/2/");
        Assert.Equal(["Остров 25", "Остров 26"], doc.QuerySelectorAll(".post-list-entry-title h2 a").Select(a => a.TextContent));
        Assert.Equal("/ru/archipelago/", doc.QuerySelector(".pagination a.prev")!.GetAttribute("href"));
    }

    [Fact]
    public async Task Article()
    {
        var doc = await _client.GetDocumentAsync("/ru/article/about/");
        Assert.Equal("О проекте", doc.QuerySelector("h1.article-title")!.TextContent);
        Assert.Equal("кто-я", doc.QuerySelector("article h2")!.Id);
        // "about" page has a big header with this item active
        Assert.Contains("active", doc.QuerySelector("header nav a[href='/ru/article/about/']")!.ClassName);
    }

    [Fact]
    public async Task EnglishPost()
    {
        var doc = await _client.GetDocumentAsync(HelsinkiEnPath);
        Assert.Equal("en", doc.DocumentElement.GetAttribute("lang"));
        Assert.Equal("Helsinki", doc.QuerySelector("h1")!.TextContent);
        // link to a post with no English version is unwrapped
        Assert.Empty(doc.QuerySelectorAll("article a"));
        Assert.Contains("See also Turku.", doc.QuerySelector("article")!.TextContent);
        // English localization
        Assert.Contains("Published on", doc.QuerySelector(".notes")!.TextContent);
    }

    [Fact]
    public async Task PostJson()
    {
        var response = await _client.GetAsync("/ru/2020/05/01/helsinki.json", Ct);
        response.EnsureSuccessStatusCode();
        Assert.Equal("application/json", response.Content.Headers.ContentType!.MediaType);
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct)).RootElement;

        Assert.Equal(HelsinkiId, json.GetProperty("id").GetInt32());
        Assert.Equal("Хельсинки", json.GetProperty("title").GetString());
        Assert.Equal($"{Url}hash{LandscapeId}/landscape.d.jpg", json.GetProperty("titleImage").GetString());
        Assert.Equal("Столица <strong>Финляндии</strong> и её крупнейший город.", json.GetProperty("description").GetString());

        var geo = json.GetProperty("geo");
        Assert.Equal(2, geo.GetArrayLength());
        // geo point: own picture (details size), description formatted, post link resolved
        Assert.Equal($"{Url}hash{PortraitId}/portrait.d.jpg", geo[0].GetProperty("titleImage").GetString());
        Assert.Equal("Главная площадь, см. <strong>собор</strong>", geo[0].GetProperty("description").GetString());
        Assert.Equal(TurkuPath, geo[0].GetProperty("links")[0].GetProperty("path").GetString());
        // second point falls back to post title picture
        Assert.Equal($"{Url}hash{LandscapeId}/landscape.d.jpg", geo[1].GetProperty("titleImage").GetString());
    }

    [Fact]
    public async Task Rss()
    {
        var response = await _client.GetAsync("/ru/rss.xml", Ct);
        response.EnsureSuccessStatusCode();
        Assert.Equal("application/rss+xml", response.Content.Headers.ContentType!.MediaType);
        var rss = XDocument.Parse(await response.Content.ReadAsStringAsync(Ct));

        var items = rss.Descendants("item").ToList();
        Assert.Equal(24, items.Count);
        var helsinki = items[1];
        Assert.Equal("Хельсинки", helsinki.Element("title")!.Value);
        Assert.Equal("https://fennica.pohjoiseen.fi" + HelsinkiPath, helsinki.Element("link")!.Value);
        Assert.Equal("Столица <strong>Финляндии</strong> и её крупнейший город.", helsinki.Element("description")!.Value);
        var enclosure = helsinki.Element("enclosure")!;
        Assert.Equal($"{Url}hash{LandscapeId}/landscape.jpg", enclosure.Attribute("url")!.Value);
        Assert.Equal("image/jpeg", enclosure.Attribute("type")!.Value);
    }

    [Theory]
    [InlineData("/old/helsinki", HelsinkiPath + "#история-города")]
    [InlineData("/old/helsinki/", HelsinkiPath + "#история-города")]
    [InlineData("/old/about", "/ru/article/about/")]
    [InlineData("/old/external", "https://example.com/x")]
    public async Task Redirects(string from, string to)
    {
        var response = await factory.CreateNonRedirectingClient().GetAsync(from, Ct);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(to, Uri.UnescapeDataString(response.Headers.Location!.ToString()));
    }

    [Theory]
    [InlineData("/css/style.css", "text/css", false)]
    [InlineData("/js/bundle.js", "text/javascript", false)]
    [InlineData("/js/bundle.js", "text/javascript", true)]
    [InlineData("/logo.svg", "image/svg+xml", false)]
    public async Task StaticFiles(string url, string contentType, bool gzip)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        if (gzip)
        {
            request.Headers.AcceptEncoding.ParseAdd("gzip");
        }
        var response = await _client.SendAsync(request, Ct);
        response.EnsureSuccessStatusCode();
        Assert.Equal(contentType, response.Content.Headers.ContentType!.MediaType);
        Assert.Equal(gzip ? ["gzip"] : [], response.Content.Headers.ContentEncoding);
        Assert.True((await response.Content.ReadAsByteArrayAsync(Ct)).Length > 1000);
    }
}

/// <summary>
/// Fennica3 as it runs inside KoTi for previews: drafts are visible.
/// </summary>
public class PageWithDraftsTests(Fennica3WithDraftsFactory factory) : IClassFixture<Fennica3WithDraftsFactory>
{
    [Fact]
    public async Task DraftsAreShown()
    {
        var client = factory.CreateClient();
        var doc = await client.GetDocumentAsync("/ru/");
        Assert.Equal("Черновик", doc.QuerySelector(".post-list-entry-title h2 a")!.TextContent);

        await client.GetDocumentAsync(DraftPostPath);
        await client.GetDocumentAsync("/ru/article/draft-article/");
    }
}
