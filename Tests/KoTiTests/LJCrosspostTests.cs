using System.Net;
using Fennica3;
using KoTi.LiveJournal;
using Microsoft.Extensions.DependencyInjection;
using Tests.Infrastructure;
using static Tests.Infrastructure.TestData;

namespace Tests.KoTiTests;

/// <summary>
/// LiveJournal crosspost HTML.  Markdown goes through the real ContentFormatter, so that these break if its output
/// changes in a way LJHtmlConverter doesn't expect.
/// </summary>
public class LJCrosspostTests(KoTiFactory factory) : IClassFixture<KoTiFactory>
{
    private const string Base = "https://fennica.pohjoiseen.fi";
    private const string PostUrl = Base + HelsinkiPath;
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    // pictures as they come out, 3000x2000 and 1500x1000 have the same web size
    private static readonly Holvi.Models.Picture P1 = Picture(LandscapeId, "landscape.jpg", 3000, 2000, true);
    private static readonly Holvi.Models.Picture P7 = Picture(Landscape2Id, "landscape2.jpg", 1500, 1000, true);
    private static readonly Holvi.Models.Picture P3 = Picture(PortraitId, "portrait.jpg", 2000, 3000, true);

    private static string Img(Holvi.Models.Picture p, string? alt = null, int w = 1015, int h = 677) =>
        $"<img {(alt is null ? "" : $"alt=\"{alt}\" ")}srcset=\"{p.Website1xUrl}, {p.Website2xUrl} 2x\" width=\"{w}\" height=\"{h}\" loading=\"lazy\">";

    private static string Pic(Holvi.Models.Picture p, string? alt = null) => $"<a href=\"{p.Url}\">{Img(p, alt)}</a>";

    /// <summary>
    /// Everything off that adds something around the content.
    /// </summary>
    private static LJCrosspostOptions Bare() => new() { TitlePicture = false, LJCut = false, LinkToOriginalPost = false };

    private async Task<string> Convert(string markdown, LJCrosspostOptions? options = null, string? titlePicture = null)
    {
        using var scope = factory.Services.CreateScope();
        var formatter = scope.ServiceProvider.GetRequiredService<ContentFormatter>();
        var html = await formatter.FormatMarkdownAsync(LJHtmlConverter.PrepareMarkdown(markdown), "ru");
        var titleHtml = titlePicture is null ? null : await formatter.FormatHTMLAsync(titlePicture, "ru");
        return LJHtmlConverter.Convert(html, titleHtml, Base, PostUrl, "ru", options ?? Bare());
    }

    /// <summary>
    /// Converted HTML without the &lt;lj-raw&gt; wrapper (checked in its own test).
    /// </summary>
    private async Task<string> Body(string markdown, LJCrosspostOptions? options = null)
    {
        var html = await Convert(markdown, options);
        Assert.StartsWith("<lj-raw>", html);
        Assert.EndsWith("</lj-raw>", html);
        return html["<lj-raw>".Length..^"</lj-raw>".Length];
    }

    // --- basics and HTML shortening

    [Fact]
    public async Task WrappedInLJRaw()
    {
        Assert.Equal("<lj-raw><p>Текст</lj-raw>", await Convert("Текст"));
    }

    [Fact]
    public async Task ShortTagsAndNoClosingParagraphs()
    {
        Assert.Equal("<p><b>жирный</b> и <i>курсив</i><br>\nстрока\n<ul>\n<li>один</li>\n</ul>\n<hr>\n<p>Конец",
            await Body("**жирный** и *курсив*  \nстрока\n\n* один\n\n---\n\nКонец"));
    }

    [Fact]
    public async Task TextAndAttributesAreEscaped()
    {
        Assert.Equal("<p><a href=\"https://example.com/?a=1&amp;b=&quot;2&quot;\">a &amp; b &lt; c &gt; d</a>",
            await Body("<a href=\"https://example.com/?a=1&amp;b=&quot;2&quot;\">a &amp; b &lt; c &gt; d</a>"));
    }

    [Fact]
    public async Task CommentsAreRemoved()
    {
        Assert.Equal("<p>Текст", await Body("<!-- заметка -->\nТекст"));
    }

    [Fact]
    public async Task SpaceAfterRemovedCommentIsKept()
    {
        Assert.Equal("<p>слово <b>жирное</b>\n<p>\u00a0", await Body("слово<!-- c --> **жирное**\n\n&#160;"));
    }

    [Fact]
    public async Task EmptyNonVoidElementsAreClosed()
    {
        Assert.Equal("<p><a id=\"x\"></a>Текст", await Body("<a id=\"x\"></a>Текст"));
    }

    // --- pictures

    [Fact]
    public async Task PictureWithoutCaptionIsMergedIntoNextParagraph()
    {
        Assert.Equal($"<p>{Pic(P1)}<br>Текст после", await Body($"![](picture:{LandscapeId})\n\nТекст после"));
    }

    [Fact]
    public async Task PictureWithCaptionIsOwnParagraph()
    {
        Assert.Equal($"<p>{Pic(P1, "Подпись")}<br><i>Подпись</i>\n<p>Текст после",
            await Body($"![Подпись](picture:{LandscapeId})\n\nТекст после"));
    }

    [Fact]
    public async Task CaptionUrlsAreLinks()
    {
        Assert.Equal($"<p>{Pic(P1, "Источник: https://example.com/x")}<br><i>Источник: <a href=\"https://example.com/x\">https://example.com/x</a></i>",
            await Body($"![Источник: https://example.com/x](picture:{LandscapeId})"));
    }

    [Fact]
    public async Task PictureAtEndIsOwnParagraph()
    {
        Assert.Equal($"<p>Текст\n<p>{Pic(P1)}", await Body($"Текст\n\n![](picture:{LandscapeId})"));
    }

    [Fact]
    public async Task PictureBeforeHeadingIsOwnParagraph()
    {
        Assert.Equal($"<p>{Pic(P1)}\n<h2 id=\"заголовок\">Заголовок</h2>",
            await Body($"![](picture:{LandscapeId})\n\n## Заголовок"));
    }

    [Fact]
    public async Task ConsecutivePicturesAreNotMergedIntoEachOther()
    {
        Assert.Equal($"<p>{Pic(P1)}<p>{Pic(P7)}<br>Текст",
            await Body($"![](picture:{LandscapeId})\n![](picture:{Landscape2Id})\n\nТекст"));
    }

    [Fact]
    public async Task CaptionedPictureIsNotMergedInto()
    {
        Assert.Equal($"<p>{Pic(P1)}<p>{Pic(P7, "Подпись")}<br><i>Подпись</i>",
            await Body($"![](picture:{LandscapeId})\n![Подпись](picture:{Landscape2Id})"));
    }

    [Fact]
    public async Task PictureInMiddleOfParagraph()
    {
        Assert.Equal($"<p>До\n<p>{Pic(P1)}<br>\nпосле",
            await Body($"До\n![](picture:{LandscapeId})\nпосле"));
    }

    [Fact]
    public async Task PortraitPicture()
    {
        Assert.Equal($"<p><a href=\"{P3.Url}\">{Img(P3, w: 677, h: 1015)}</a>", await Body($"![](picture:{PortraitId})"));
    }

    [Fact]
    public async Task SmallPictureHasOnlySrcAndNoLink()
    {
        Assert.Equal($"<p><img src=\"{Url}hash{SmallPngId}/small.png\" width=\"800\" height=\"600\" loading=\"lazy\">",
            await Body($"![](picture:{SmallPngId})"));
    }

    [Fact]
    public async Task RawPictureIsKeptButCustomAttributesAreRemoved()
    {
        Assert.Equal("<img src=\"https://example.com/x.jpg\" alt=\"x\">",
            await Body("<img raw=\"raw\" src=\"https://example.com/x.jpg\" alt=\"x\" />"));
    }

    [Theory]
    [InlineData(LJImageSourceMode.SrcAndSrcset, "src=\"{1x}\" srcset=\"{1x}, {2x} 2x\" ")]
    [InlineData(LJImageSourceMode.SrcsetOnly, "srcset=\"{1x}, {2x} 2x\" ")]
    [InlineData(LJImageSourceMode.Src2x, "src=\"{2x}\" ")]
    public async Task ImageSource(LJImageSourceMode mode, string expected)
    {
        var options = Bare();
        options.ImageSource = mode;
        Assert.Equal($"<p><a href=\"{P1.Url}\"><img {expected.Replace("{1x}", P1.Website1xUrl).Replace("{2x}", P1.Website2xUrl)}width=\"1015\" height=\"677\" loading=\"lazy\"></a>",
            await Body($"![](picture:{LandscapeId})", options));
    }

    [Fact]
    public async Task Src2xFallsBackToOriginalIfNo2x()
    {
        var options = Bare();
        options.ImageSource = LJImageSourceMode.Src2x;
        Assert.Contains($"<img src=\"{Url}hash{Only1xId}/only1x.jpg\" width=\"967\" height=\"677\"",
            await Body($"![](picture:{Only1xId})", options));
    }

    [Fact]
    public async Task RemoveAlts()
    {
        var options = Bare();
        options.RemoveAlts = true;
        Assert.Equal($"<p>{Pic(P1)}<br><i>Подпись</i>", await Body($"![Подпись](picture:{LandscapeId})", options));
    }

    [Fact]
    public async Task RemoveLazyLoading()
    {
        var options = Bare();
        options.RemoveLazyLoading = true;
        Assert.Equal($"<p>{Pic(P1).Replace(" loading=\"lazy\"", "")}", await Body($"![](picture:{LandscapeId})", options));
        Assert.Equal("<img src=\"https://example.com/x.jpg\">", await Body("<img raw=\"\" src=\"https://example.com/x.jpg\" loading=\"lazy\" />", options));
    }

    [Fact]
    public async Task NoLinksToOriginals()
    {
        var options = Bare();
        options.LinkPicturesToOriginals = false;
        Assert.Equal($"<p>{Img(P1, "Подпись")}<br><i>Подпись</i>\n<p>{Img(P7)}<br>Текст со <a href=\"https://example.com/\">ссылкой</a>",
            await Body($"![Подпись](picture:{LandscapeId})\n\n![](picture:{Landscape2Id})\n\nТекст со [ссылкой](https://example.com/)", options));
    }

    // --- title picture

    private static readonly string TitlePicture = $"<p><img alt=\"\" src=\"picture:{LandscapeId}\" /></p>";

    [Fact]
    public async Task TitlePictureIsOwnParagraph()
    {
        var options = Bare();
        options.TitlePicture = true;
        Assert.Equal($"<lj-raw><p>{Pic(P1)}\n<p>Текст</lj-raw>", await Convert("Текст", options, TitlePicture));
    }

    [Fact]
    public async Task TitlePictureWithCaption()
    {
        var options = Bare();
        options.TitlePicture = true;
        Assert.Equal($"<lj-raw><p>{Pic(P1, "Вид")}<br><i>Вид</i>\n<p>Текст</lj-raw>",
            await Convert("Текст", options, $"<p><img alt=\"Вид\" src=\"picture:{LandscapeId}\" /></p>"));
    }

    [Fact]
    public async Task TitlePictureOff()
    {
        Assert.Equal("<lj-raw><p>Текст</lj-raw>", await Convert("Текст", Bare(), TitlePicture));
    }

    // --- galleries

    private const string Gallery3 = "<!--gallery-->\n![Собор](picture:1)\n![](picture:7)\n![Река](picture:3)\n<!--/gallery-->";
    private const string Gallery4 = "<!--gallery-->\n![](picture:1)\n![](picture:7)\n![](picture:1)\n![](picture:7)\n<!--/gallery-->";

    [Fact]
    public async Task GalleryAsIndividualPictures()
    {
        Assert.Equal($"<p>Текст\n<p>{Pic(P1, "Собор")}<br><i>Собор</i><p>{Pic(P7)}<p><a href=\"{P3.Url}\">{Img(P3, "Река", 677, 1015)}</a><br><i>Река</i>\n<p>После",
            await Body($"Текст\n\n{Gallery3}\n\nПосле"));
    }

    [Fact]
    public async Task GalleryAsIndividualPicturesMergesLastIntoParagraph()
    {
        Assert.Equal($"<p>{Pic(P1)}<p>{Pic(P7)}<br>После",
            await Body($"<!--gallery-->\n![](picture:1)\n![](picture:7)\n<!--/gallery-->\n\nПосле"));
    }

    [Fact]
    public async Task LJGallery()
    {
        var options = Bare();
        options.Galleries = LJGalleryMode.LJGallery;
        Assert.Equal($"<p>Текст\n<lj-gallery width=\"1015\" height=\"677\">\n" +
                     $"<lj-gallery-item src=\"{P1.Website2xUrl}\">Собор</lj-gallery-item>\n" +
                     $"<lj-gallery-item src=\"{P7.Website2xUrl}\"></lj-gallery-item>\n" +
                     $"<lj-gallery-item src=\"{P3.Website2xUrl}\">Река</lj-gallery-item>\n" +
                     $"</lj-gallery>\n<p>После",
            await Body($"Текст\n\n{Gallery3}\n\nПосле", options));
    }

    [Theory]
    [InlineData(Gallery3, false)]
    [InlineData(Gallery4, true)]
    public async Task LJGalleryIfMoreThan3(string gallery, bool isLJGallery)
    {
        var options = Bare();
        options.Galleries = LJGalleryMode.LJGalleryIfMoreThan3;
        var html = await Body(gallery, options);
        Assert.Equal(isLJGallery, html.Contains("<lj-gallery "));
        Assert.Equal(isLJGallery ? 0 : 3, html.Split("<img ").Length - 1);
    }

    [Fact]
    public async Task SeveralGalleries()
    {
        var options = Bare();
        options.Galleries = LJGalleryMode.LJGallery;
        var html = await Body($"{Gallery3}\n\nТекст\n\n{Gallery4}", options);
        Assert.Equal(2, html.Split("<lj-gallery ").Length - 1);
        Assert.Equal(7, html.Split("<lj-gallery-item ").Length - 1);
        Assert.Contains("</lj-gallery>\n<p>Текст\n<lj-gallery ", html);
    }

    [Fact]
    public async Task UnclosedGalleryIsIndividualPictures()
    {
        var options = Bare();
        options.Galleries = LJGalleryMode.LJGallery;
        Assert.Equal($"<p>{Pic(P1)}", await Body("<!--gallery-->\n![](picture:1)", options));
    }

    // --- asides

    private const string Asides = "Текст\n\n<!--aside-->\nЗаметка\n<!--/aside-->\n\n<!--aside wide-->\nШирокая\n<!--/aside-->\n\nКонец";

    [Fact]
    public async Task AsidesAreRemoved()
    {
        Assert.Equal("<p>Текст\n<p>Конец", await Body(Asides));
    }

    [Fact]
    public async Task AsidesAsBlockquotes()
    {
        var options = Bare();
        options.Asides = LJAsideMode.Blockquote;
        Assert.Equal("<p>Текст\n<blockquote>\n<p>Заметка\n</blockquote>\n<blockquote>\n<p>Широкая\n</blockquote>\n<p>Конец",
            await Body(Asides, options));
    }

    [Fact]
    public async Task PictureInAsideBlockquote()
    {
        var options = Bare();
        options.Asides = LJAsideMode.Blockquote;
        Assert.Equal($"<blockquote>\n<p>{Pic(P1)}<br>Заметка\n</blockquote>",
            await Body("<!--aside-->\n![](picture:1)\n\nЗаметка\n<!--/aside-->", options));
    }

    // --- links and headings

    [Fact]
    public async Task UrlsAreAbsolute()
    {
        Assert.Equal($"<p><a href=\"{Base}{TurkuPath}#история\">Турку</a> <a href=\"{Base}/ru/article/about/\">о проекте</a> " +
                     "<a href=\"https://example.com/x\">x</a> <a href=\"#якорь\">тут</a> <a href=\"//example.com/\">y</a>",
            await Body($"<a href=\"post:{TurkuId}#история\">Турку</a> [о проекте](article:{AboutId}) [x](https://example.com/x) " +
                       "<a href=\"#якорь\">тут</a> <a href=\"//example.com/\">y</a>"));
    }

    [Fact]
    public async Task RelativeImageUrlsAreAbsolute()
    {
        var options = Bare();
        options.ImageSource = LJImageSourceMode.SrcAndSrcset;
        Assert.Equal($"<img src=\"{Base}/favicon.png\" srcset=\"{Base}/a.png, {Base}/b.png 2x\">",
            await Body("<img raw=\"\" src=\"/favicon.png\" srcset=\"/a.png, /b.png 2x\" />", options));
    }

    [Fact]
    public async Task NonAsciiLettersInUrlsAreDecoded()
    {
        // Markdig percent-encodes these, also already encoded ones stay so
        Assert.Equal($"<p><a href=\"https://ru.wikipedia.org/wiki/Хельсинки_(город)\">a</a> " +
                     $"<a href=\"{Base}/ru/article/about/#кто-я\">b</a> <a href=\"https://fi.wikipedia.org/wiki/Hämeenlinna\">c</a>",
            await Body($"[a](https://ru.wikipedia.org/wiki/Хельсинки_(город)) [b](article:{AboutId}#кто-я) " +
                       "[c](https://fi.wikipedia.org/wiki/H%C3%A4meenlinna)"));
    }

    [Theory]
    [InlineData("https://example.com/a%20b%23c")]        // ASCII stays encoded
    [InlineData("https://example.com/%C2%A0x")]          // so do non-letters (nbsp)
    [InlineData("https://example.com/%E2%80%94")]        // (em dash)
    [InlineData("https://example.com/%D0%A5%D0")]        // invalid UTF-8, also latin-1 encoded
    [InlineData("https://example.com/%E4")]
    public async Task OtherUrlEncodingIsKept(string url)
    {
        Assert.Equal($"<p><a href=\"{url}\">x</a>", await Body($"<a href=\"{url}\">x</a>"));
    }

    [Fact]
    public async Task NonAsciiLettersInPictureUrlsAreDecoded()
    {
        var options = Bare();
        options.ImageSource = LJImageSourceMode.SrcAndSrcset;
        Assert.Equal("<img src=\"https://example.com/Ä.jpg\" srcset=\"https://example.com/Ä.jpg, https://example.com/Ö.jpg 2x\">",
            await Body("<img raw=\"\" src=\"https://example.com/%C3%84.jpg\" srcset=\"https://example.com/%C3%84.jpg, https://example.com/%C3%96.jpg 2x\" />", options));
    }

    [Fact]
    public async Task OtherRelativeUrlsAreResolvedAgainstPostUrl()
    {
        Assert.Equal($"<p><a href=\"{PostUrl}images/x.png\">x</a> <a href=\"{Base}/ru/2020/05/01/y\">y</a>",
            await Body("[x](./images/x.png) [y](../y)"));
    }

    [Fact]
    public async Task StylesAndScriptsAreRemoved()
    {
        Assert.Equal("<p>Текст", await Body("<style>\np { color: red; }\n</style>\n\nТекст\n\n<script>alert(1);</script>"));
    }

    [Fact]
    public async Task HeadingIdsCanBeRemoved()
    {
        var options = Bare();
        Assert.Equal("<h2 id=\"история\">История</h2>\n<h3 id=\"x\">X</h3>", await Body("## История\n\n<h3 id=\"x\">X</h3>", options));
        options.HeadingIds = false;
        Assert.Equal("<h2>История</h2>\n<h3>X</h3>", await Body("## История\n\n<h3 id=\"x\">X</h3>", options));
    }

    // --- LJ-cut and link to original

    private static string OriginalLink => $"<p><i>Оригинал поста: <a href=\"{PostUrl}\">{PostUrl}</a></i>";

    [Fact]
    public async Task LJCutAfterFirstParagraph()
    {
        var options = Bare();
        options.LJCut = true;
        Assert.Equal("<lj-raw><p>Первый\n<lj-cut>\n<p>Второй\n<p>Третий\n</lj-cut></lj-raw>",
            await Convert("Первый\n\nВторой\n\nТретий", options));
    }

    [Fact]
    public async Task LJCutAfterFirstParagraphNotHeading()
    {
        var options = Bare();
        options.LJCut = true;
        Assert.Equal("<lj-raw><h2 id=\"заголовок\">Заголовок</h2>\n<p>Первый\n<lj-cut>\n<p>Второй\n</lj-cut></lj-raw>",
            await Convert("## Заголовок\n\nПервый\n\nВторой", options));
    }

    [Fact]
    public async Task LJCutIncludesPictureMergedIntoFirstParagraph()
    {
        var options = Bare();
        options.LJCut = true;
        Assert.Equal($"<lj-raw><p>{Pic(P1)}<br>Первый\n<lj-cut>\n<p>Второй\n</lj-cut></lj-raw>",
            await Convert("![](picture:1)\n\nПервый\n\nВторой", options));
    }

    [Fact]
    public async Task NoLJCutIfNothingToCut()
    {
        var options = Bare();
        options.LJCut = true;
        Assert.Equal("<lj-raw><p>Первый</lj-raw>", await Convert("Первый", options));
    }

    [Fact]
    public async Task LinkToOriginalInsideLJCut()
    {
        var options = new LJCrosspostOptions { TitlePicture = false };
        Assert.Equal($"<lj-raw><p>Первый\n<lj-cut>\n<p>Второй\n{OriginalLink}\n</lj-cut></lj-raw>",
            await Convert("Первый\n\nВторой", options));
    }

    [Fact]
    public async Task LinkToOriginalWithoutLJCut()
    {
        var options = new LJCrosspostOptions { TitlePicture = false, LJCut = false };
        Assert.Equal($"<lj-raw><p>Первый\n<p>Второй\n{OriginalLink}</lj-raw>", await Convert("Первый\n\nВторой", options));
        // also when there's nothing to cut
        options.LJCut = true;
        Assert.Equal($"<lj-raw><p>Первый\n{OriginalLink}</lj-raw>", await Convert("Первый", options));
    }

    [Fact]
    public async Task LinkToOriginalInOtherLanguages()
    {
        var html = LJHtmlConverter.Convert("<p>Text</p>", null, Base, Base + HelsinkiEnPath, "en", new LJCrosspostOptions());
        Assert.Contains($"<p><i>Original post: <a href=\"{Base}{HelsinkiEnPath}\">", html);
    }

    // --- newlines

    [Fact]
    public async Task RemoveNewlines()
    {
        var options = new LJCrosspostOptions { RemoveNewlines = true };
        Assert.Equal($"<lj-raw><p>{Pic(P1)}<p>Первая строка вторая <i>а</i> <b>б</b><lj-cut><ul><li>один</li><li>два</li></ul>" +
                     $"<p>{Pic(P7, "Подпись")}<br><i>Подпись</i><pre><code>код\nкод\n</code></pre>{OriginalLink}</lj-cut></lj-raw>",
            await Convert("Первая строка\nвторая *а*\n**б**\n\n* один\n* два\n\n![Подпись](picture:7)\n\n```\nкод\nкод\n```",
                options, TitlePicture));
    }

    // --- whole post via LJCrosspostFormatter, all defaults

    [Fact]
    public async Task PostWithDefaults()
    {
        using var scope = factory.Services.CreateScope();
        var formatter = scope.ServiceProvider.GetRequiredService<LJCrosspostFormatter>();
        var html = await formatter.FormatAsync(HelsinkiId, "ru", new LJCrosspostOptions());

        const string nbsp = " ";
        Assert.Equal($"<lj-raw><p>{Pic(P1)}\n" +
                     $"<h2 id=\"история-города\">История города</h2>\n" +
                     $"<p>Хельсинки основан в{nbsp}1550{nbsp}году{nbsp}— это <a href=\"{Base}{TurkuPath}\">Турку</a> был столицей раньше.\n" +
                     $"<lj-cut>\n" +
                     $"<p>{Pic(P1, "Вид на город")}<br><i>Вид на город</i>\n" +
                     $"<p>{Pic(P1)}<p>{Pic(P7)}\n" +
                     $"{OriginalLink}\n" +
                     $"</lj-cut></lj-raw>",
            html);
    }

    [Fact]
    public async Task PostNotFound()
    {
        using var scope = factory.Services.CreateScope();
        var formatter = scope.ServiceProvider.GetRequiredService<LJCrosspostFormatter>();
        Assert.Null(await formatter.FormatAsync(9999, "ru", new LJCrosspostOptions()));
        Assert.Null(await formatter.FormatAsync(HelsinkiId, "en", new LJCrosspostOptions()));
    }

    [Fact]
    public async Task PostInBookLinksToBookUrl()
    {
        using var scope = factory.Services.CreateScope();
        var formatter = scope.ServiceProvider.GetRequiredService<LJCrosspostFormatter>();
        Assert.Equal($"<lj-raw><p>Озеро Инари.\n<p><i>Оригинал поста: <a href=\"{Base}{InariPath}\">{Base}{InariPath}</a></i></lj-raw>",
            await formatter.FormatAsync(InariId, "ru", new LJCrosspostOptions()));
    }

    // --- over HTTP

    private readonly HttpClient _htmx = factory.CreateHtmxClient();

    [Fact]
    public async Task EditPageHasButtonOnlyForPosts()
    {
        var client = factory.CreateNonRedirectingClient();
        var post = await client.GetDocumentAsync($"/Posts/{HelsinkiId}/ru");
        Assert.Equal($"/Posts/{HelsinkiId}/ru/LJ/", post.QuerySelector("#lj-crosspost-button")!.GetAttribute("hx-get"));
        Assert.NotNull(post.QuerySelector("#lj-crosspost-container"));

        var article = await client.GetDocumentAsync($"/Articles/{AboutId}/ru");
        Assert.Null(article.QuerySelector("#lj-crosspost-button"));
    }

    [Fact]
    public async Task Dialog()
    {
        var response = await _htmx.GetAsync($"/Posts/{TurkuId}/ru/LJ", Ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("dialogopenmodal", response.Headers.GetValues("HX-Trigger-After-Swap").Single());

        var doc = await _htmx.GetDocumentAsync($"/Posts/{TurkuId}/ru/LJ");
        var component = doc.QuerySelector("koti-lj-crosspost")!;
        Assert.Equal($"/Posts/{TurkuId}/ru/LJ/Html/", component.GetAttribute("html-url"));
        Assert.NotNull(doc.QuerySelector("dialog#lj-crosspost-dialog"));

        // HTML with default settings, and default settings in the form
        var html = doc.QuerySelector($"textarea#{component.GetAttribute("initial-html-id")}")!.TextContent;
        Assert.StartsWith("<lj-raw><p>Турку", html);
        Assert.DoesNotContain("<lj-gallery", html);
        Assert.True(doc.QuerySelector("input[name=TitlePicture]")!.HasAttribute("checked"));
        Assert.False(doc.QuerySelector("input[name=RemoveNewlines]")!.HasAttribute("checked"));
        Assert.Equal("Individual", doc.QuerySelector("select[name=Galleries] option[selected]")!.GetAttribute("value"));
        Assert.Equal("SrcsetOnly", doc.QuerySelector("select[name=ImageSource] option[selected]")!.GetAttribute("value"));
        Assert.False(doc.QuerySelector("input[name=RemoveLazyLoading]")!.HasAttribute("checked"));
        Assert.Equal(11, doc.QuerySelectorAll("form.lj-crosspost-options [name]").Length);
    }

    [Fact]
    public async Task HtmlWithSettings()
    {
        var response = await _htmx.GetAsync($"/Posts/{TurkuId}/ru/LJ/Html?TitlePicture=false&Galleries=LJGallery&LJCut=false" +
                                             "&LinkToOriginalPost=false&RemoveNewlines=true&ImageSource=Src2x", Ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/plain", response.Content.Headers.ContentType!.MediaType);
        Assert.Equal($"<lj-raw><p>Турку — старейший город. Назад в <a href=\"https://fennica.pohjoiseen.fi/ru/2020/05/01/helsinki/#история-города\">Хельсинки</a>." +
                     "<lj-gallery width=\"1015\" height=\"677\">" +
                     $"<lj-gallery-item src=\"{P1.Website2xUrl}\">Собор</lj-gallery-item>" +
                     $"<lj-gallery-item src=\"{P7.Website2xUrl}\">Замок</lj-gallery-item>" +
                     $"<lj-gallery-item src=\"{P3.Website2xUrl}\">Река</lj-gallery-item>" +
                     "</lj-gallery></lj-raw>",
            await response.Content.ReadAsStringAsync(Ct));
    }

    [Theory]
    [InlineData("/Posts/9999/ru/LJ")]
    [InlineData("/Posts/1/en/LJ")]
    [InlineData("/Posts/9999/ru/LJ/Html")]
    public async Task NotFound(string url)
    {
        Assert.Equal(HttpStatusCode.NotFound, (await _htmx.GetAsync(url, Ct)).StatusCode);
    }
}
