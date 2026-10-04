using System.Net;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Fennica3;
using Holvi;
using KoTi.LiveJournal;
using Markdig;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tests.Infrastructure;

namespace Tests.RealData;

/// <summary>
/// Fennica3 running read-only against a real database, set in POHJOISEEN_TEST_DB environment variable.
/// </summary>
public class RealDatabaseFactory : Fennica3Factory
{
    public static readonly string? Path = Environment.GetEnvironmentVariable("POHJOISEEN_TEST_DB");

    protected override IDictionary<string, string?> GetSettings()
    {
        var settings = base.GetSettings();
        // goes into "Data Source=..." connection string as is
        settings["Holvi:DatabaseFile"] = $"{Path};Mode=ReadOnly";
        return settings;
    }
}

/// <summary>
/// Renders all content in a real database, to catch content that breaks with formatter changes (or was broken
/// all along).  Skipped unless POHJOISEEN_TEST_DB is set, e.g.:
///   POHJOISEEN_TEST_DB=$PWD/pohjoiseen.db dotnet test --project Tests -- --filter-class Tests.RealData.RealDatabaseTests
/// </summary>
public class RealDatabaseTests(RealDatabaseFactory factory) : IClassFixture<RealDatabaseFactory>
{
    private static readonly bool Enabled = RealDatabaseFactory.Path is not null;

    [Fact]
    public async Task AllContentRenders()
    {
        Assert.SkipUnless(Enabled, "POHJOISEEN_TEST_DB not set");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HolviDbContext>();
        var formatter = scope.ServiceProvider.GetRequiredService<ContentFormatter>();

        var items = new List<(string Name, string Language, string Markdown)>();
        items.AddRange((await db.Posts.AsNoTracking().ToListAsync(TestContext.Current.CancellationToken))
            .SelectMany(p => new[]
            {
                ($"post {p.Id} {p}", p.Language, p.ContentMD),
                ($"post {p.Id} {p} description", p.Language, p.Description)
            }));
        items.AddRange((await db.Articles.AsNoTracking().ToListAsync(TestContext.Current.CancellationToken))
            .Select(a => ($"article {a.Id} {a.Name}", a.Language, a.ContentMD)));
        items.AddRange((await db.Books.AsNoTracking().ToListAsync(TestContext.Current.CancellationToken))
            .Select(b => ($"book {b.Id} {b.Name}", b.Language, b.ContentMD)));

        var failures = new List<string>();
        var parser = new HtmlParser();
        foreach (var (name, language, markdown) in items)
        {
            try
            {
                var html = await formatter.FormatMarkdownAsync(markdown, language);

                // formatting may add text (captions, link URLs) but should never lose any
                // (Markdig's plain text renderer keeps raw HTML as is, so strip tags, comments, and styles/scripts)
                var sourceText = WebUtility.HtmlDecode(Regex.Replace(Markdown.ToPlainText(markdown),
                    "<!--.*?-->|<(style|script)[^>]*>.*?</\\1>|<[^>]*>", "", RegexOptions.Singleline));
                var document = parser.ParseDocument(html);
                var renderedText = document.Body!.TextContent;
                if (renderedText.Count(char.IsLetter) < sourceText.Count(char.IsLetter))
                {
                    var rendered = Words(renderedText).CountBy(w => w).ToDictionary();
                    var missing = Words(sourceText).CountBy(w => w)
                        .Where(w => w.Value > rendered.GetValueOrDefault(w.Key))
                        .Select(w => w.Key).Take(10);
                    failures.Add($"{name}: text lost: {string.Join(" ", missing)}");
                }

                foreach (var image in document.QuerySelectorAll("img[src^='picture:']:not([raw])"))
                {
                    failures.Add($"{name}: picture not found: {image.GetAttribute("src")}");
                }

                // only post:, article: and book: links are resolved
                foreach (var link in document.QuerySelectorAll("a[href^='picture:']"))
                {
                    failures.Add($"{name}: unresolved link: {link.GetAttribute("href")}");
                }
            }
            catch (Exception e)
            {
                failures.Add($"{name}: {e.GetType().Name}: {e.Message}");
            }
        }

        Assert.True(failures.Count == 0, $"{failures.Count} of {items.Count} failed to render:\n" + string.Join("\n", failures));
    }

    [Fact]
    public async Task AllPostsConvertForLJ()
    {
        Assert.SkipUnless(Enabled, "POHJOISEEN_TEST_DB not set");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<HolviDbContext>();
        var formatter = scope.ServiceProvider.GetRequiredService<ContentFormatter>();
        var ljFormatter = ActivatorUtilities.CreateInstance<LJCrosspostFormatter>(scope.ServiceProvider);

        // defaults, and settings that keep all content
        var defaults = new LJCrosspostOptions();
        var everything = new LJCrosspostOptions
        {
            Galleries = LJGalleryMode.LJGallery, Asides = LJAsideMode.Blockquote, RemoveNewlines = true,
            ImageSource = LJImageSourceMode.Src2x
        };

        var posts = await db.Posts.AsNoTracking().OrderBy(p => p.Id).ToListAsync(TestContext.Current.CancellationToken);
        var failures = new List<string>();
        var sizes = new List<(int Bytes, string Name)>();
        var parser = new HtmlParser();
        foreach (var post in posts)
        {
            var name = $"post {post.Id} {post}";
            try
            {
                foreach (var options in new[] { defaults, everything })
                {
                    var html = (await ljFormatter.FormatAsync(post.Id, post.Language, options))!;
                    if (options == defaults)
                    {
                        sizes.Add((System.Text.Encoding.UTF8.GetByteCount(html), name));
                    }

                    if (!html.StartsWith("<lj-raw>") || !html.EndsWith("</lj-raw>"))
                    {
                        failures.Add($"{name}: not wrapped in lj-raw");
                    }
                    foreach (var leftover in new[] { "</p>", "/>", "<!--", "<figure", "<aside", "glider", "<strong", "<em>" })
                    {
                        if (html.Contains(leftover))
                        {
                            failures.Add($"{name}: contains {leftover}");
                        }
                    }
                    if (options.RemoveNewlines && Regex.IsMatch(html, "\n(?![^<]*</code>)"))
                    {
                        failures.Add($"{name}: newlines left");
                    }

                    var document = parser.ParseDocument(html);
                    var urls = document.QuerySelectorAll("a[href]").Select(a => a.GetAttribute("href")!)
                        .Concat(document.QuerySelectorAll("img[src], lj-gallery-item").Select(i => i.GetAttribute("src")!))
                        .Concat(document.QuerySelectorAll("img[srcset]")
                            .SelectMany(i => i.GetAttribute("srcset")!.Split(',').Select(c => c.Trim().Split(' ')[0])));
                    // ([text](picture:XXX) links are not resolved by ContentFormatter, reported by AllContentRenders)
                    foreach (var url in urls.Where(u => !Regex.IsMatch(u, "^(https?://|#|mailto:|picture:)")))
                    {
                        failures.Add($"{name}: not absolute URL: {url}");
                    }

                    // nothing may be lost compared to what is shown on the blog, if all content is kept
                    if (options == everything)
                    {
                        // (text nodes joined with spaces: without newlines, words in separate paragraphs are not separated,
                        // neither are Glider gallery captions on the blog; style is not text)
                        static string Text(IElement element) => string.Join(" ", element.GetDescendants().OfType<IText>()
                            .Where(t => t.ParentElement?.LocalName != "style").Select(t => t.Data));
                        var blogText = Text(parser.ParseDocument(await formatter.FormatMarkdownAsync(post.ContentMD, post.Language)).Body!);
                        var ljWords = Words(Text(document.Body!)).CountBy(w => w).ToDictionary();
                        var missing = Words(blogText).CountBy(w => w)
                            .Where(w => w.Value > ljWords.GetValueOrDefault(w.Key))
                            .Select(w => w.Key).Take(10).ToList();
                        if (missing.Count > 0)
                        {
                            failures.Add($"{name}: text lost: {string.Join(" ", missing)}");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                failures.Add($"{name}: {e.GetType().Name}: {e.Message}");
            }
        }

        TestContext.Current.TestOutputHelper?.WriteLine("LJ HTML sizes with default settings, largest: " +
            string.Join(", ", sizes.OrderByDescending(s => s.Bytes).Take(10).Select(s => $"{s.Name} {s.Bytes}")) +
            $"; over 64 KB: {sizes.Count(s => s.Bytes > 65535)} of {sizes.Count}");
        Assert.True(failures.Count == 0, $"{failures.Count} problems in {posts.Count} posts:\n" + string.Join("\n", failures));
    }

    private static IEnumerable<string> Words(string text) =>
        Regex.Matches(text, @"\p{L}+").Select(m => m.Value);
}
