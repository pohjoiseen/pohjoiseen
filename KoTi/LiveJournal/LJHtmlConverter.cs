using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Holvi.Models;

namespace KoTi.LiveJournal;

/// <summary>
/// Converts post HTML, as rendered by Fennica3 ContentFormatter, into HTML for crossposting to LiveJournal:
/// pictures as paragraphs instead of figure/figcaption, absolute URLs, &lt;lj-raw&gt;, &lt;lj-cut&gt;,
/// &lt;lj-gallery&gt;, and markup made as short as possible, since LJ posts are limited to 64 KB.
/// Pure function of its input, the database part is in <see cref="LJCrosspostFormatter"/>.
/// </summary>
public static class LJHtmlConverter
{
    // galleries are passed through ContentFormatter under different markers, so that it doesn't build Glider
    // markup and pictures stay as figures (see PrepareMarkdown)
    private const string GalleryStart = "lj-gallery", GalleryEnd = "/lj-gallery";

    // <lj-gallery> size: height of website size pictures and 3:2 aspect ratio
    public const int GalleryHeight = Picture.WebsiteSize;
    public const int GalleryWidth = GalleryHeight * 3 / 2;

    private static readonly HashSet<string> VoidElements =
        ["area", "base", "br", "col", "embed", "hr", "img", "input", "link", "meta", "source", "track", "wbr"];

    // elements around which whitespace doesn't matter, for RemoveNewlines
    private static readonly HashSet<string> BlockElements =
    [
        "html", "lj-raw", "lj-cut", "lj-gallery", "lj-gallery-item", "p", "div", "h1", "h2", "h3", "h4", "h5", "h6", "ul", "ol", "li",
        "dl", "dt", "dd", "blockquote", "figure", "figcaption", "table", "thead", "tbody", "tfoot", "tr", "td", "th",
        "caption", "hr", "br", "aside", "section", "iframe"
    ];

    private static readonly Dictionary<string, string> OriginalPostLabels = new()
    {
        ["ru"] = "Оригинал поста",
        ["en"] = "Original post",
        ["fi"] = "Alkuperäinen kirjoitus"
    };

    /// <summary>
    /// Preprocess post Markdown before it is given to ContentFormatter.
    /// </summary>
    public static string PrepareMarkdown(string markdown) => markdown
        .Replace("<!--gallery-->", $"<!--{GalleryStart}-->")
        .Replace("<!--/gallery-->", $"<!--{GalleryEnd}-->");

    /// <summary>
    /// Convert formatted post HTML to LJ HTML.
    /// </summary>
    /// <param name="contentHtml">Post content, from ContentFormatter, Markdown preprocessed with PrepareMarkdown</param>
    /// <param name="titlePictureHtml">Title picture as figure from ContentFormatter, null if none</param>
    /// <param name="baseUrl">Public URL of the blog, prepended to relative links</param>
    /// <param name="postUrl">Absolute URL of the post</param>
    /// <param name="language">Post language</param>
    /// <param name="options">Settings</param>
    /// <returns>HTML to paste into LJ</returns>
    public static string Convert(string contentHtml, string? titlePictureHtml, string baseUrl, string postUrl,
                                 string language, LJCrosspostOptions options)
    {
        var content = Parse(contentHtml);
        ConvertGalleries(content, options);
        foreach (var comment in content.DescendantNodes().OfType<XComment>().ToList())
        {
            comment.Remove();
        }
        // LJ doesn't allow these anyway
        content.Descendants().Where(e => e.Name.LocalName is "style" or "script").Remove();
        ConvertAsides(content, options);
        Process(content, baseUrl, postUrl, options);

        var root = new XElement("lj-raw");
        if (titlePictureHtml is not null && options.TitlePicture)
        {
            var titlePicture = Parse(titlePictureHtml);
            Process(titlePicture, baseUrl, postUrl, options);
            root.Add(titlePicture.Nodes().Where(n => !IsWhitespace(n)), "\n");
        }

        // everything up to and including the first paragraph is shown before the cut
        var nodes = content.Nodes().SkipWhile(IsWhitespace).ToList();
        var firstParagraph = nodes.FindIndex(n => n is XElement { Name.LocalName: "p" });
        var beforeCut = options.LJCut && firstParagraph >= 0 ? nodes.Take(firstParagraph + 1).ToList() : nodes;
        var afterCut = nodes.Skip(beforeCut.Count).SkipWhile(IsWhitespace).ToList();
        foreach (var node in nodes)
        {
            node.Remove();
        }
        TrimEnd(beforeCut);
        TrimEnd(afterCut);

        var originalLink = options.LinkToOriginalPost
            ? new XElement("p", new XElement("i",
                $"{OriginalPostLabels.GetValueOrDefault(language, OriginalPostLabels["ru"])}: ",
                new XElement("a", new XAttribute("href", postUrl), postUrl)))
            : null;

        root.Add(beforeCut);
        if (afterCut.Count > 0)
        {
            root.Add("\n", new XElement("lj-cut", "\n", afterCut, originalLink is null ? null : new object[] { "\n", originalLink }, "\n"));
        }
        else if (originalLink is not null)
        {
            root.Add("\n", originalLink);
        }

        // removed elements and comments may leave consecutive whitespace
        foreach (var text in root.DescendantNodes().OfType<XText>()
                     .Where(t => IsWhitespace(t) && t.PreviousNode is XText previous && IsWhitespace(previous)).ToList())
        {
            text.Remove();
        }

        if (options.RemoveNewlines)
        {
            RemoveNewlines(root);
        }

        var output = new StringBuilder();
        Serialize(root, output);
        return output.ToString();
    }

    private static XElement Parse(string html) => XElement.Parse("<html>" + html + "</html>", LoadOptions.PreserveWhitespace);

    // (not String.IsNullOrWhiteSpace, nbsp is not whitespace here)
    private static bool IsWhitespace(XNode node) => node is XText text && text.Value.All(c => c is ' ' or '\t' or '\r' or '\n');

    private static void TrimEnd(List<XNode> nodes)
    {
        while (nodes.Count > 0 && IsWhitespace(nodes[^1]))
        {
            nodes.RemoveAt(nodes.Count - 1);
        }
    }

    /// <summary>
    /// Everything except galleries and asides, used both for content and title picture.
    /// </summary>
    private static void Process(XElement document, string baseUrl, string postUrl, LJCrosspostOptions options)
    {
        ConvertFigures(document, options);
        foreach (var image in document.Descendants("img"))
        {
            ConvertImage(image, options);
        }
        ConvertUrls(document, baseUrl, postUrl);

        if (!options.HeadingIds)
        {
            foreach (var heading in document.Descendants().Where(e => Regex.IsMatch(e.Name.LocalName, "^h[1-6]$")))
            {
                heading.SetAttributeValue("id", null);
            }
        }

        foreach (var element in document.Descendants().ToList())
        {
            element.Name = element.Name.LocalName switch
            {
                "strong" => "b",
                "em" => "i",
                _ => element.Name
            };
        }
    }

    /// <summary>
    /// Handle &lt;!--lj-gallery--&gt;...&lt;!--/lj-gallery--&gt; blocks, either as &lt;lj-gallery&gt;
    /// or just removing the markers.
    /// </summary>
    private static void ConvertGalleries(XElement document, LJCrosspostOptions options)
    {
        foreach (var start in document.DescendantNodes().OfType<XComment>().Where(c => c.Value == GalleryStart).ToList())
        {
            // markers must be siblings (they normally are, as separate HTML blocks in Markdown)
            var end = start.NodesAfterSelf().OfType<XComment>().FirstOrDefault(c => c.Value == GalleryEnd);
            if (end is null)
            {
                continue;
            }

            var nodes = start.NodesAfterSelf().TakeWhile(n => n != end).ToList();
            var images = nodes.OfType<XElement>().SelectMany(e => e.DescendantsAndSelf("img")).ToList();
            var useLJGallery = options.Galleries switch
            {
                LJGalleryMode.LJGallery => true,
                LJGalleryMode.LJGalleryIfMoreThan3 => images.Count > 3,
                _ => false
            };
            if (!useLJGallery || images.Count == 0)
            {
                continue;
            }

            // like Glider galleries in Fennica3, only pictures are used, anything else in gallery block is dropped
            var gallery = new XElement("lj-gallery",
                new XAttribute("width", GalleryWidth), new XAttribute("height", GalleryHeight));
            foreach (var image in images)
            {
                var caption = image.Ancestors("figure").FirstOrDefault()?.Element("figcaption")?.Value
                              ?? image.Attribute("alt")?.Value ?? "";
                gallery.Add("\n", new XElement("lj-gallery-item",
                    new XAttribute("src", Get2xSource(image) ?? image.Attribute("src")?.Value ?? ""),
                    caption.Trim()));
            }
            gallery.Add("\n");

            foreach (var node in nodes)
            {
                node.Remove();
            }
            start.ReplaceWith(gallery);
            end.Remove();
        }
    }

    private static void ConvertAsides(XElement document, LJCrosspostOptions options)
    {
        foreach (var aside in document.Descendants("aside").ToList())
        {
            // ContentFormatter puts non-wide asides in a wrapper
            XElement asideOrWrapper = aside.Parent is { Name.LocalName: "div" } wrapper
                                      && wrapper.Attribute("class")?.Value == "aside-wrapper"
                                      && wrapper.Elements().Count() == 1
                ? wrapper
                : aside;
            if (options.Asides == LJAsideMode.Blockquote)
            {
                asideOrWrapper.ReplaceWith(new XElement("blockquote", aside.Nodes()));
            }
            else
            {
                asideOrWrapper.Remove();
            }
        }
    }

    /// <summary>
    /// &lt;figure&gt;&lt;a&gt;&lt;img&gt;&lt;/a&gt;&lt;figcaption&gt;Caption&lt;/figcaption&gt;&lt;/figure&gt; to
    /// &lt;p&gt;&lt;a&gt;&lt;img&gt;&lt;/a&gt;&lt;br&gt;&lt;i&gt;Caption&lt;/i&gt;&lt;/p&gt;; without caption,
    /// merges picture into the following paragraph (&lt;p&gt;&lt;a&gt;&lt;img&gt;&lt;/a&gt;&lt;br&gt;Text&lt;/p&gt;).
    /// </summary>
    private static void ConvertFigures(XElement document, LJCrosspostOptions options)
    {
        // paragraphs made from figures, these are never merged into
        var pictureParagraphs = new HashSet<XElement>();

        foreach (var figure in document.Descendants("figure").ToList())
        {
            var image = figure.Descendants("img").FirstOrDefault();
            if (image is null)
            {
                figure.ReplaceWith(figure.Nodes());
                continue;
            }

            // link to original picture
            var picture = options.LinkPicturesToOriginals
                          && image.Parent is { Name.LocalName: "a" } link && link.Parent == figure
                ? link
                : image;
            picture.Remove();

            var caption = figure.Element("figcaption");
            if (caption is not null && !String.IsNullOrWhiteSpace(caption.Value))
            {
                var paragraph = new XElement("p", picture, new XElement("br"), new XElement("i", caption.Nodes()));
                figure.ReplaceWith(paragraph);
                pictureParagraphs.Add(paragraph);
                continue;
            }

            var next = figure.NodesAfterSelf().FirstOrDefault(n => !IsWhitespace(n));
            if (next is XElement { Name.LocalName: "p" } nextParagraph && !pictureParagraphs.Contains(nextParagraph))
            {
                nextParagraph.AddFirst(picture, new XElement("br"));
                // also whitespace between figure and paragraph
                foreach (var node in figure.NodesAfterSelf().TakeWhile(n => n != nextParagraph).ToList())
                {
                    node.Remove();
                }
                figure.Remove();
                pictureParagraphs.Add(nextParagraph);
            }
            else
            {
                var paragraph = new XElement("p", picture);
                figure.ReplaceWith(paragraph);
                pictureParagraphs.Add(paragraph);
            }
        }
    }

    private static void ConvertImage(XElement image, LJCrosspostOptions options)
    {
        // non-standard attributes for ContentFormatter
        image.SetAttributeValue("raw", null);
        image.SetAttributeValue("nofigure", null);

        if (options.RemoveAlts || String.IsNullOrWhiteSpace(image.Attribute("alt")?.Value))
        {
            image.SetAttributeValue("alt", null);
        }

        if (options.RemoveLazyLoading)
        {
            image.SetAttributeValue("loading", null);
        }

        if (image.Attribute("srcset") is null)
        {
            return;
        }

        switch (options.ImageSource)
        {
            case LJImageSourceMode.SrcsetOnly:
                image.SetAttributeValue("src", null);
                break;
            case LJImageSourceMode.Src2x:
                var src2x = Get2xSource(image);
                if (src2x is not null)
                {
                    image.SetAttributeValue("src", src2x);
                }
                image.SetAttributeValue("srcset", null);
                break;
        }
    }

    /// <summary>
    /// URL of 2x version of a picture from srcset, null if there is none.
    /// </summary>
    private static string? Get2xSource(XElement image)
    {
        var srcset = image.Attribute("srcset")?.Value;
        if (srcset is null)
        {
            return null;
        }

        foreach (var candidate in srcset.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = candidate.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2 && parts[1] == "2x")
            {
                return parts[0];
            }
        }

        return null;
    }

    /// <summary>
    /// Make URLs absolute, and percent-decode non-ASCII letters (Markdig encodes Cyrillic in URLs, 6 bytes per letter
    /// instead of 2).
    /// </summary>
    private static void ConvertUrls(XElement document, string baseUrl, string postUrl)
    {
        string Convert(string url) => DecodeNonAsciiLetters(MakeAbsolute(url));

        // links to anchors within the post are left as is, they work on LJ too (if heading ids are kept)
        string MakeAbsolute(string url) => url switch
        {
            _ when url.StartsWith("//") || url.StartsWith('#') || Regex.IsMatch(url, "^[a-zA-Z][a-zA-Z0-9+.-]*:") => url,
            _ when url.StartsWith('/') => baseUrl.TrimEnd('/') + url,
            // other relative URLs are rare (old content), resolve them like a browser would on the blog
            _ => new Uri(new Uri(postUrl), url).AbsoluteUri
        };

        foreach (var attribute in document.Descendants().Attributes().ToList())
        {
            switch (attribute.Name.LocalName)
            {
                case "href" or "src":
                    attribute.Value = Convert(attribute.Value);
                    break;
                case "srcset":
                    attribute.Value = String.Join(", ", attribute.Value
                        .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                        .Select(candidate => Convert(candidate)));
                    break;
            }
        }
    }

    private static string DecodeNonAsciiLetters(string url) =>
        // runs of percent-encoded non-ASCII bytes, decoded only if valid UTF-8 and only letters, marks and digits
        // (ASCII like %20 or %23 stays encoded, it has a meaning in URLs)
        Regex.Replace(url, "(?:%[89A-Fa-f][0-9A-Fa-f])+", match =>
        {
            var bytes = Enumerable.Range(0, match.Length / 3)
                .Select(i => System.Convert.ToByte(match.Value.Substring(i * 3 + 1, 2), 16)).ToArray();
            try
            {
                var decoded = new UTF8Encoding(false, true).GetString(bytes);
                return Regex.IsMatch(decoded, @"^[\p{L}\p{M}\p{N}]+$") ? decoded : match.Value;
            }
            catch (DecoderFallbackException)
            {
                return match.Value;
            }
        });

    /// <summary>
    /// Replace newlines with spaces, and remove whitespace where it doesn't matter (around block elements).
    /// </summary>
    private static void RemoveNewlines(XElement root)
    {
        bool IsBlock(XNode? node) => node is XElement element && BlockElements.Contains(element.Name.LocalName);

        foreach (var text in root.DescendantNodes().OfType<XText>().ToList())
        {
            if (text.Ancestors("pre").Any())
            {
                continue;
            }

            var value = Regex.Replace(text.Value, "[ \t\r\n]*\n[ \t\r\n]*", " ");
            if (IsBlock(text.PreviousNode) || (text.PreviousNode is null && IsBlock(text.Parent)))
            {
                value = value.TrimStart(' ', '\t', '\r', '\n');
            }
            if (IsBlock(text.NextNode) || (text.NextNode is null && IsBlock(text.Parent)))
            {
                value = value.TrimEnd(' ', '\t', '\r', '\n');
            }

            if (value.Length == 0)
            {
                text.Remove();
            }
            else
            {
                text.Value = value;
            }
        }
    }

    /// <summary>
    /// Write out HTML (not XHTML), as short as possible: no closing &lt;/p&gt;, no "/&gt;" on void elements,
    /// no comments.
    /// </summary>
    private static void Serialize(XElement element, StringBuilder output)
    {
        var name = element.Name.LocalName;
        output.Append('<').Append(name);
        foreach (var attribute in element.Attributes())
        {
            output.Append(' ').Append(attribute.Name.LocalName).Append("=\"")
                .Append(attribute.Value.Replace("&", "&amp;").Replace("\"", "&quot;")).Append('"');
        }
        output.Append('>');

        if (VoidElements.Contains(name))
        {
            return;
        }

        foreach (var node in element.Nodes())
        {
            switch (node)
            {
                case XElement child:
                    Serialize(child, output);
                    break;
                case XText text:
                    output.Append(text.Value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;"));
                    break;
            }
        }

        if (name != "p")
        {
            output.Append("</").Append(name).Append('>');
        }
    }
}
