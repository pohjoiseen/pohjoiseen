using System.Text.RegularExpressions;

namespace Teos;

/// <summary>
/// Typographic prettyfing of HTML.
/// </summary>
public class TypographyFormatter : IContentFormatter
{
    public Task<string> FormatHTML(string html, string path)
    {
        // TODO: the only thing we do right now is convert double dashes into proper long dash (adding a nbsp before it)
        // And even so this is probably not super reliable...
        html = Regex.Replace(html, "([^-!/<>])--([^-<>])", "$1—$2", RegexOptions.Singleline);
        html = Regex.Replace(html, "\\s—", "\u00a0—");
        return Task.FromResult(html);
    }
}