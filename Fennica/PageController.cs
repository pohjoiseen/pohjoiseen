using System.Text.RegularExpressions;
using Razor.Templating.Core;
using Teos;

namespace Fennica;

/// <summary>
/// Controller for Page content.
/// </summary>
public class PageController : FennicaController
{
    public override IList<string> GetRoutes(Content content)
    {
        return new List<string>() { content.CanonicalURL };
    }

    public override async Task Render(Content content, GroupCollection routeMatch, HttpResponse response)
    {
        var page = (Page)content;
        SetLanguage(page);

        var layoutParams = new LayoutParams()
        {
            Title = page.Title,
            Language = page.Language,
            BodyClass = "body-article",
            LanguageVersions = GetLanguageVersionURLs(page.Name)
        };
        if (page.Name.Contains("/about.")) layoutParams.BigHeaderPage = "about";
        if (page.Name.Contains("/contents.")) layoutParams.BigHeaderPage = "contents";

        await response.WriteAsync(await RazorTemplateEngine.RenderAsync("/Views/Page.cshtml", page, new Dictionary<string, object>()
        {
            { "LayoutParams", layoutParams }
        }));
    }
}