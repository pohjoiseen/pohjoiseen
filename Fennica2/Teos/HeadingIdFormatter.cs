using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Teos;

/// <summary>
/// Adds id attributes to all h1...h6 tags, with tag contents as id (lowercased and with whitespace and other
/// characters replaced by dashes).
/// </summary>
public class HeadingIdFormatter : IContentFormatter
{
    /// <inheritdoc cref="IContentFormatter.FormatHTML"/>
    public Task<string> FormatHTML(string html, string path)
    {
        // use LINQ to XML, same technique as LinkAndImageFormatter
        var wrappedHtml = "<html>" + html + "</html>";
        var document = XElement.Parse(wrappedHtml, LoadOptions.PreserveWhitespace);

        for (int level = 1; level <= 6; level++)
        {
            foreach (var heading in from heading in document.Descendants($"h{level}") select heading)
            {
                if (heading.Attribute("id") == null)
                {
                    string value = heading.Value;
                    heading.SetAttributeValue("id", Regex.Replace(value.ToLowerInvariant(), "\\W+", "-", RegexOptions.Singleline));
                }
            }                
        }

        wrappedHtml = document.ToString(SaveOptions.DisableFormatting);
        return Task.FromResult(wrappedHtml.Replace("<html>", "").Replace("</html>", ""));
    }
}