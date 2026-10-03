using Fennica3;
using Microsoft.Extensions.DependencyInjection;
using Tests.Infrastructure;
using static Tests.Infrastructure.TestData;

namespace Tests.Fennica3Tests;

/// <summary>
/// Markdown/HTML in, expected HTML out.  ContentFormatter is resolved from a running Fennica3 app,
/// since it needs the database and routing (for link generation).
/// </summary>
public class ContentFormatterTests(Fennica3Factory factory) : IClassFixture<Fennica3Factory>
{
    private const string Nbsp = " ";

    private async Task<string> Format(string markdown, string language = "ru", bool singlePara = false)
    {
        using var scope = factory.Services.CreateScope();
        var formatter = scope.ServiceProvider.GetRequiredService<ContentFormatter>();
        return await formatter.FormatMarkdownAsync(markdown, language, singlePara);
    }

    private async Task<string> FormatHtml(string html, string language = "ru")
    {
        using var scope = factory.Services.CreateScope();
        var formatter = scope.ServiceProvider.GetRequiredService<ContentFormatter>();
        return await formatter.FormatHTMLAsync(html, language);
    }

    // --- basics

    [Fact]
    public async Task PlainMarkdown()
    {
        Assert.Equal("<p>Hello <em>world</em></p>\n", await Format("Hello *world*"));
    }

    [Fact]
    public async Task SingleParagraphIsUnwrapped()
    {
        Assert.Equal("Hello <strong>world</strong>", await Format("Hello **world**", singlePara: true));
    }

    [Fact]
    public async Task SingleParagraphLeavesOtherBlocksAlone()
    {
        Assert.Equal("<ul>\n<li>one</li>\n</ul>", await Format("* one", singlePara: true));
    }

    [Fact]
    public async Task HandwrittenHtmlIsKept()
    {
        Assert.Equal("<div class=\"x\"><span>a</span></div>\n",
            await Format("<div class=\"x\"><span>a</span></div>"));
    }

    // --- headings

    [Fact]
    public async Task HeadingsGetIds()
    {
        Assert.Equal("<h2 id=\"история-города\">История города</h2>\n<h3 id=\"part-two-the-end-\">Part two: the end!</h3>\n",
            await Format("## История города\n\n### Part two: the end!"));
    }

    [Fact]
    public async Task ExplicitHeadingIdIsKept()
    {
        Assert.Equal("<h2 id=\"custom\">Title</h2>", await FormatHtml("<h2 id=\"custom\">Title</h2>"));
    }

    // --- post:, article:, book: links

    [Fact]
    public async Task PostLink()
    {
        Assert.Equal($"<a href=\"{TurkuPath}\">Турку</a>", await Format($"[Турку](post:{TurkuId})", singlePara: true));
    }

    [Fact]
    public async Task PostLinkWithAnchor()
    {
        Assert.Equal($"<a href=\"{TurkuPath}#history\">Турку</a>",
            await Format($"[Турку](post:{TurkuId}#history)", singlePara: true));
    }

    [Fact]
    public async Task PostInBookLink()
    {
        Assert.Equal($"<a href=\"{InariPath}\">Инари</a>", await Format($"[Инари](post:{InariId})", singlePara: true));
    }

    [Fact]
    public async Task PostLinkResolvesToSamePostInContentLanguage()
    {
        // Russian post ID in English content links to English version of the same post (same date and name)...
        Assert.Equal($"<a href=\"{HelsinkiEnPath}\">x</a>", await Format($"[x](post:{HelsinkiId})", "en", true));
        // ...and vice versa
        Assert.Equal($"<a href=\"{HelsinkiPath}\">x</a>", await Format($"[x](post:{HelsinkiEnId})", "ru", true));
    }

    [Fact]
    public async Task PostLinkWithoutVersionInContentLanguageIsUnwrapped()
    {
        Assert.Equal("see Turku", await Format($"see [Turku](post:{TurkuId})", "en", true));
    }

    [Fact]
    public async Task MissingPostLinkIsUnwrapped()
    {
        Assert.Equal("see <em>this</em> post", await Format("see [*this*](post:9999) post", singlePara: true));
    }

    [Fact]
    public async Task DraftPostLinkIsUnwrapped()
    {
        Assert.Equal("draft", await Format($"[draft](post:{DraftPostId})", singlePara: true));
    }

    [Fact]
    public async Task MalformedPostLinkIsLeftAsIs()
    {
        Assert.Equal("<a href=\"post:abc\">x</a>", await Format("[x](post:abc)", singlePara: true));
    }

    [Fact]
    public async Task ArticleLink()
    {
        Assert.Equal("<a href=\"/ru/article/about/#who\">about</a>",
            await Format($"[about](article:{AboutId}#who)", singlePara: true));
    }

    [Fact]
    public async Task LinkAnchorIsUrlEncodedByMarkdig()
    {
        // browsers match percent-encoded fragment against ids just fine
        Assert.Equal("<a href=\"/ru/article/about/#%D0%BA%D1%82%D0%BE-%D1%8F\">about</a>",
            await Format($"[about](article:{AboutId}#кто-я)", singlePara: true));
    }

    [Fact]
    public async Task DraftOrMissingArticleLinkIsUnwrapped()
    {
        Assert.Equal("a b", await Format($"[a](article:{DraftArticleId}) [b](article:9999)", singlePara: true));
    }

    [Fact]
    public async Task BookLink()
    {
        Assert.Equal("<a href=\"/ru/lapland/\">book</a> secret",
            await Format($"[book](book:{LaplandBookId}) [secret](book:{DraftBookId})", singlePara: true));
    }

    [Fact]
    public async Task OrdinaryLinksAreLeftAlone()
    {
        Assert.Equal("<a href=\"https://example.com/a--b\">x</a> <a href=\"/ru/\">y</a>",
            await Format("[x](https://example.com/a--b) [y](/ru/)", singlePara: true));
    }

    // --- pictures

    [Fact]
    public async Task PictureWithWebsiteSizes()
    {
        var p = Picture(LandscapeId, "landscape.jpg", 3000, 2000, true);
        Assert.Equal(
            $"<figure><a href=\"{p.Url}\"><img src=\"{p.Website1xUrl}\" alt=\"Вид\" srcset=\"{p.Website1xUrl}, {p.Website2xUrl} 2x\" width=\"1015\" height=\"677\" loading=\"lazy\" /></a><figcaption>Вид</figcaption></figure>\n",
            await Format($"![Вид](picture:{LandscapeId})"));
    }

    [Fact]
    public async Task PortraitPictureIsSizedByWidth()
    {
        var html = await Format($"![](picture:{PortraitId})");
        Assert.Contains("width=\"677\" height=\"1015\"", html);
    }

    [Fact]
    public async Task PictureWithOnly1xUsesOriginalAs2x()
    {
        var p = Picture(Only1xId, "only1x.jpg", 1000, 700, true, only1x: true);
        var html = await Format($"![](picture:{Only1xId})");
        Assert.Contains($"srcset=\"{p.Website1xUrl}, {p.Url} 2x\" width=\"967\" height=\"677\"", html);
    }

    [Fact]
    public async Task PictureWithoutWebsiteSizesUsesOriginal()
    {
        // without on-the-fly downsizing (production), the original is used as is, without link to itself
        var p = Picture(SmallPngId, "small.png", 800, 600, false);
        Assert.Equal(
            $"<figure><img src=\"{p.Url}\" alt=\"\" width=\"800\" height=\"600\" loading=\"lazy\" /></figure>\n",
            await Format($"![](picture:{SmallPngId})"));
    }

    [Fact]
    public async Task ParagraphIsSplitAroundPicture()
    {
        var p = Picture(SmallPngId, "small.png", 800, 600, false);
        Assert.Equal(
            $"<p>Text <em>a</em> </p><figure><img src=\"{p.Url}\" alt=\"\" width=\"800\" height=\"600\" loading=\"lazy\" /></figure><p> more</p>\n",
            await Format($"Text *a* ![](picture:{SmallPngId}) more"));
    }

    [Fact]
    public async Task TextRightAfterPictureIsKept()
    {
        // picture with caption immediately followed by text, without empty line in between
        var html = await Format($"![Caption](picture:{SmallPngId})Paragraph text\nand more.");
        Assert.EndsWith("<figcaption>Caption</figcaption></figure><p>Paragraph text\nand more.</p>\n", html);
    }

    [Fact]
    public async Task PicturesInSameParagraphAreSplit()
    {
        var html = await Format($"![](picture:{SmallPngId})\n![](picture:{CoatId})");
        Assert.Matches("^<figure><img [^>]*/></figure><figure><img [^>]*/></figure>\n$", html);
    }

    [Fact]
    public async Task PictureWithNofigure()
    {
        var p = Picture(SmallPngId, "small.png", 800, 600, false);
        Assert.Equal(
            $"<img src=\"{p.Url}\" nofigure=\"\" width=\"800\" height=\"600\" loading=\"lazy\" />",
            await FormatHtml($"<img src=\"picture:{SmallPngId}\" nofigure=\"\" />"));
    }

    [Fact]
    public async Task RawPictureIsLeftAlone()
    {
        const string html = "<img raw=\"\" src=\"picture:1\" />";
        Assert.Equal(html, await FormatHtml(html));
    }

    [Fact]
    public async Task MissingPictureIsLeftAlone()
    {
        Assert.Equal("<p><img src=\"picture:9999\" alt=\"\" /></p>\n", await Format("![](picture:9999)"));
    }

    [Fact]
    public async Task UrlsInCaptionAreLinked()
    {
        var html = await Format($"![Source: https://example.com/photo](picture:{SmallPngId})");
        Assert.Contains("<figcaption>Source: <a href=\"https://example.com/photo\">https://example.com/photo</a></figcaption>", html);
    }

    // --- galleries

    [Fact]
    public async Task FewSimilarPicturesWithoutCaptionsAreArrangedInRow()
    {
        var html = await Format($"<!--gallery-->\n![](picture:{LandscapeId})\n![](picture:{Landscape2Id})\n<!--/gallery-->");
        // just the (linked) images, no figures
        var p1 = Picture(LandscapeId, "landscape.jpg", 3000, 2000, true);
        var p2 = Picture(Landscape2Id, "landscape2.jpg", 1500, 1000, true);
        Assert.Equal(
            "<div class=\"multiple-images multiple-images-2\">" +
            $"<a href=\"{p1.Url}\"><img src=\"{p1.Website1xUrl}\" alt=\"\" srcset=\"{p1.Website1xUrl}, {p1.Website2xUrl} 2x\" width=\"1015\" height=\"677\" loading=\"lazy\" /></a>" +
            $"<a href=\"{p2.Url}\"><img src=\"{p2.Website1xUrl}\" alt=\"\" srcset=\"{p2.Website1xUrl}, {p2.Website2xUrl} 2x\" width=\"1015\" height=\"677\" loading=\"lazy\" /></a>" +
            "</div>\n",
            html);
    }

    [Theory]
    [InlineData("![caption](picture:1)\n![](picture:7)")]  // caption
    [InlineData("![](picture:1)\n![](picture:3)")]  // different aspect ratios
    [InlineData("![](picture:3)\n![](picture:3)")]  // vertical
    [InlineData("![](picture:1)\n![](picture:7)\n![](picture:1)\n![](picture:7)\n![](picture:1)")]  // more than 4
    public async Task OtherwiseGliderGalleryIsGenerated(string pictures)
    {
        var html = await Format($"<!--gallery-->\n{pictures}\n<!--/gallery-->");
        var doc = HttpClientExtensions.ParseHtml(html);
        var gallery = doc.QuerySelector("div.glider-contain[data-id='1']");
        Assert.NotNull(gallery);
        Assert.Equal(pictures.Split('\n').Length, gallery.QuerySelectorAll(".glider-image img").Length);
        Assert.NotNull(gallery.QuerySelector("#glider-prev-1"));
        Assert.NotNull(gallery.QuerySelector("#glider-next-1"));
        Assert.NotNull(gallery.QuerySelector("#glider-dots-1"));
    }

    [Fact]
    public async Task GliderGalleryMarkup()
    {
        var p = Picture(LandscapeId, "landscape.jpg", 3000, 2000, true);
        var html = await Format($"<!--gallery-->\n![Вид www.example.com](picture:{LandscapeId})\n<!--/gallery-->");
        Assert.Equal(
            "<div class=\"glider-contain\" data-id=\"1\"><div class=\"glider\">" +
            "<div class=\"glider-image\">" +
            $"<a class=\"glider-external-link\" href=\"{p.Url}\"><i class=\"fas fa-external-link-alt\"></i></a>" +
            "<div class=\"glider-image-wrap\">" +
            $"<img loading=\"lazy\" src=\"{p.Website1xUrl}\" srcset=\"{p.Website1xUrl}, {p.Website2xUrl} 2x\" />" +
            "<p>Вид <a href=\"http://www.example.com\">www.example.com</a></p>" +
            "</div></div></div>" +
            "<button id=\"glider-prev-1\" role=\"button\" aria-label=\"Previous\" class=\"glider-prev\"><i class=\"fa fa-chevron-left\"></i></button>" +
            "<button id=\"glider-next-1\" role=\"button\" aria-label=\"Next\" class=\"glider-next\"><i class=\"fa fa-chevron-right\"></i></button>" +
            "<div id=\"glider-dots-1\" class=\"glider-dots\"></div></div>\n",
            html);
    }

    [Fact]
    public async Task MultipleGalleriesAreNumbered()
    {
        var gallery = $"<!--gallery-->\n![a](picture:{LandscapeId})\n<!--/gallery-->";
        var html = await Format($"{gallery}\n\nText\n\n{gallery}");
        Assert.Contains("data-id=\"1\"", html);
        Assert.Contains("data-id=\"2\"", html);
        Assert.Contains("<p>Text</p>", html);
        Assert.DoesNotContain("gallery-->", html);
    }

    // --- asides

    [Fact]
    public async Task AsideIsWrapped()
    {
        Assert.Equal("<div class=\"aside-wrapper\"><aside>\n<p>Note</p>\n</aside></div>\n",
            await Format("<!--aside-->\nNote\n<!--/aside-->"));
    }

    [Fact]
    public async Task WideAsideIsNotWrapped()
    {
        Assert.Equal("<aside class=\"wide\">\n<p>Note</p>\n</aside>\n",
            await Format("<!--aside wide-->\nNote\n<!--/aside-->"));
    }

    // --- typography

    [Fact]
    public async Task DoubleDashBecomesEmDashWithNbsp()
    {
        Assert.Equal($"Хельсинки{Nbsp}— столица", await Format("Хельсинки -- столица", singlePara: true));
    }

    [Fact]
    public async Task DoubleDashInUrlsIsKept()
    {
        Assert.Equal("<a href=\"https://xn--80ak6aa92e.com/\">https://xn--80ak6aa92e.com/</a>",
            await Format("<https://xn--80ak6aa92e.com/>", singlePara: true));
    }

    [Theory]
    [InlineData("высота 1324 м", "высота" + Nbsp + "1324" + Nbsp + "м")]
    [InlineData("в 1550 году", "в" + Nbsp + "1550" + Nbsp + "году")]
    [InlineData("12,5 км", "12,5" + Nbsp + "км")]
    [InlineData("2 кв. км", "2" + Nbsp + "кв." + Nbsp + "км")]
    [InlineData("5 млн. человек", "5" + Nbsp + "млн. человек")]
    [InlineData("5 мм", "5" + Nbsp + "мм")]
    [InlineData("5 матрёшек", "5 матрёшек")]
    [InlineData("Пётр I и Карл XII", "Пётр" + Nbsp + "I и Карл" + Nbsp + "XII")]
    public async Task NoBreaksAroundNumbersAndUnits(string input, string expected)
    {
        Assert.Equal(expected, await Format(input, singlePara: true));
    }

    // --- everything together, as in a real post

    [Fact]
    public async Task FullPost()
    {
        var html = await Format(HelsinkiContent);
        var doc = HttpClientExtensions.ParseHtml(html);

        Assert.Equal("история-города", doc.QuerySelector("h2")!.Id);
        Assert.Equal(TurkuPath, doc.QuerySelector("a[href^='/ru/2020']")!.GetAttribute("href"));
        Assert.Contains($"1550{Nbsp}году{Nbsp}— это", html);
        Assert.Equal("Вид на город", doc.QuerySelector("figure > figcaption")!.TextContent);
        Assert.NotNull(doc.QuerySelector("div.multiple-images-2"));
        Assert.Equal("/ru/article/about/", doc.QuerySelector(".aside-wrapper > aside a")!.GetAttribute("href"));
    }
}

/// <summary>
/// In KoTi previews, drafts are linked as well.
/// </summary>
public class ContentFormatterWithDraftsTests(Fennica3WithDraftsFactory factory) : IClassFixture<Fennica3WithDraftsFactory>
{
    [Fact]
    public async Task DraftsAreLinked()
    {
        using var scope = factory.Services.CreateScope();
        var formatter = scope.ServiceProvider.GetRequiredService<ContentFormatter>();
        Assert.Equal(
            $"<a href=\"{DraftPostPath}\">a</a> <a href=\"/ru/article/draft-article/\">b</a> <a href=\"/ru/secret-book/\">c</a>",
            await formatter.FormatMarkdownAsync(
                $"[a](post:{DraftPostId}) [b](article:{DraftArticleId}) [c](book:{DraftBookId})", "ru", true));
    }
}
