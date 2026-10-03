using System.Net;
using System.Text.RegularExpressions;
using AngleSharp.Html.Parser;
using Fennica3;
using Holvi;
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

    private static IEnumerable<string> Words(string text) =>
        Regex.Matches(text, @"\p{L}+").Select(m => m.Value);
}
