using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Holvi;
using Holvi.Models;
using Markdig;
using Microsoft.EntityFrameworkCore;

namespace Fennica3;

/// <summary>
/// Formats Markdown (or HTML) for display in Fennica3.
/// Most importantly, resolves "post:XXX" and "picture:XXX" links, and generates correct
/// markup for images (with figure/figcaption, srcset and such.)
/// Pretty much the most critical part of Fennica3 webapp :)
/// </summary>
/// <param name="dbContext">Database</param>
/// <param name="pictureUpload">Picture upload handler</param>
/// <param name="helpers">Helpers class (for link generation)</param>
/// <param name="logger">.NET logger</param>
/// <param name="configuration">App configuration</param>
public class ContentFormatter(HolviDbContext dbContext, PictureUpload pictureUpload,
                              Helpers helpers, ILogger<ContentFormatter> logger, IConfiguration configuration)
{
    /// <summary>
    /// Format and postprocess Markdown content.
    /// </summary>
    /// <param name="markdown">Markdown to render</param>
    /// <param name="language">Language of the content</param>   
    /// <param name="singlePara">if true, won't wrap result in any block tags</param>
    /// <returns>HTML ready to display</returns>
    public async Task<string> FormatMarkdownAsync(string markdown, string language, bool singlePara = false)
    {
        // we just go ahead with Markdig defaults
        var html = await FormatHTMLAsync(Markdown.ToHtml(markdown), language);
        
        // unwrap from "<p>...</p>" if that was supposed to be only one paragraph
        if (singlePara)
        {
            html = html.Trim();
            if (html.StartsWith("<p>"))
            {
                html = html.Substring(3);
            }

            if (html.EndsWith("</p>"))
            {
                html = html.Substring(0, html.Length - 4);
            }
        }

        return html;
    }
    
    /// <summary>
    /// Format and postprocess HTML content.
    /// </summary>
    /// <param name="html">Intermediate HTML</param>
    /// <param name="language">Language of the content</param>   
    /// <returns>HTML ready to display</returns>
    public async Task<string> FormatHTMLAsync(string html, string language)
    {
        html = FormatPreliminary(html);

        // use XML processing for various tasks
        // this of course means our HTML needs to be well-formed as XML.  Markdig generates correct XHTML,
        // but need to be careful with any handwritten markup in posts.
        var document = XElement.Parse("<html>" + html + "</html>", LoadOptions.PreserveWhitespace);
        await ConvertPostLinks(document, language);
        await ConvertPictureLinks(document);
        AddHeaderIds(document);
        WrapAsides(document);
        
        // convert back to a string with HTML and do some more processing
        html = document.ToString(SaveOptions.DisableFormatting)
            .Replace("<html>", "").Replace("</html>", "");
        html = FormatGalleries(html);
        html = FormatTypography(html);

        return html;
    }

    /// <summary>
    /// Resolve "post:XXX", "article:XXX" etc. links in HTML to actual post and article URLs.
    /// Only handles &lt;a href="..."&gt; attributes.
    /// </summary>
    /// <param name="document">HTML as XHTML document</param>
    /// <param name="language">Language of the content</param>   
    private async Task ConvertPostLinks(XElement document, string language)
    {
        // do it in two steps, first collect all links to be resolved
        IList<(XElement, int, string)> links = new List<(XElement, int, string)>();
        
        // get all <a> in a document
        foreach (var link in from link in document.Descendants("a") select link)
        {
            // look for href attribute
            var href = link.Attribute("href");
            if (href == null)
            {
                continue;
            }

            // handle only "post:" and "article:" links
            if (href.Value.StartsWith("post:"))
            {
                var match = Regex.Match(href.Value, "post:([0-9]+)(#.*)?");
                if (!match.Success)
                {
                    logger.LogWarning("Malformed post link: {href}", href.Value);
                    continue;
                }

                var postId = Int32.Parse(match.Groups[1].Value);
                var hash = match.Length > 1 ? match.Groups[2].Value : "";

                // request all posts at once as an optimization
                links.Add((link, postId, hash));
            }
            else if (href.Value.StartsWith("article:"))
            {
                var match = Regex.Match(href.Value, "article:([0-9]+)(#.*)?");
                if (!match.Success)
                {
                    logger.LogWarning("Malformed article link: {href}", href.Value);
                    continue;
                }

                var articleId = Int32.Parse(match.Groups[1].Value);
                var hash = match.Length > 1 ? match.Groups[2].Value : "";

                // article links are rare, look up and replace right away
                var article = dbContext.Articles.FirstOrDefault(a => a.Id == articleId);
                if (article == null)
                {
                    logger.LogWarning("Article not found for article link: {href}", href.Value);
                    continue;
                }

                link.SetAttributeValue("href", helpers.ArticleLink(article) + hash);
            }
            else if (href.Value.StartsWith("book:"))
            {
                var match = Regex.Match(href.Value, "book:([0-9]+)(#.*)?");
                if (!match.Success)
                {
                    logger.LogWarning("Malformed book link: {href}", href.Value);
                    continue;
                }

                var bookId = Int32.Parse(match.Groups[1].Value);
                var hash = match.Length > 1 ? match.Groups[2].Value : "";

                // book links are rare, look up and replace right away
                var book = dbContext.Books.FirstOrDefault(b => b.Id == bookId);
                if (book == null)
                {
                    logger.LogWarning("Book not found for book link: {href}", href.Value);
                    continue;
                }

                link.SetAttributeValue("href", helpers.BookLink(book) + hash);
            }
        }

        // second step, resolve all posts at once
        // do a little magic here with a join: allow linking to a post in a different language, but resolve
        // the same post (by date and name) always in the same language.  This means contents of several posts could be
        // just copied as is to another language, and the links between them would continue working still.
        // TODO: really need to put this to Holvi, this is relatively heavy EF Core stuff...
        var posts = await dbContext.Posts
            .Where(p => links.Select(l => l.Item2).Contains(p.Id))
            .Join(dbContext.Posts,
                p => new { p.Date, p.Name },
                pp => new { pp.Date, pp.Name },
                (p, pp) => new { Original = p, TargetLanguage = pp, Book = pp.Book })
            .Where(p => p.TargetLanguage.Language == language)
            .ToDictionaryAsync(p => p.Original.Id, p => p.TargetLanguage);
        foreach (var (link, postId, hash) in links)
        {
            if (!posts.TryGetValue(postId, out var post))
            {
                logger.LogWarning("Post ID not found: {id}", postId);
                // if not found, just unwrap link
                link.ReplaceWith(link.Nodes());
                continue;
            }

            // remove links to draft posts as well
            if (post.Draft && configuration["Fennica3:WithDrafts"] == null)
            {
                logger.LogWarning("Linked to draft Post ID: {id}", postId);
                link.ReplaceWith(link.Nodes());
                continue;
            }
            
            link.SetAttributeValue("href", helpers.PostLink(post) + hash);
        }
    }

    /// <summary>
    /// Resolve "picture:XXX" images in HTML to actual image URLs and generate wrapping figure markup.
    /// Only handles &lt;img src="..."&gt; attributes.
    /// </summary>
    /// <param name="document">HTML as XHTML document</param>
    private async Task ConvertPictureLinks(XElement document)
    {
        // do it in two steps, first collect all pictures to be resolved
        IList<(XElement, int)> images = new List<(XElement, int)>();

        // get all <img> in a document
        foreach (var image in from image in document.Descendants("img") select image)
        {
            // skip completely if (non-standard) raw attribute is set, to allow for manually crafted <img>'s
            if (image.Attribute("raw") != null)
            {
                continue;
            }

            // look for src attribute
            var src = image.Attribute("src");
            if (src == null)
            {
                continue;
            }

            // handle only "picture:" links
            if (!src.Value.StartsWith("picture:"))
            {
                continue;
            }

            var match = Regex.Match(src.Value, "picture:([0-9]+)");
            if (!match.Success)
            {
                logger.LogWarning("Malformed picture link: {src}", src.Value);
                continue;
            }

            var pictureId = Int32.Parse(match.Groups[1].Value);
            // request all pictures at once as an optimization
            images.Add((image, pictureId));
        }
        
        // second step, resolve all pictures at once
        var pictures = await dbContext.Pictures
            .Where(p => images.Select(l => l.Item2).Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);
        foreach (var (image, pictureId) in images)
        {
            if (!pictures.TryGetValue(pictureId, out var picture))
            {
                logger.LogWarning("Picture ID not found: {id}", pictureId);
                continue;
            }

            // found image
            image.SetAttributeValue("src", picture.Url);

            // website sizes should exist
            if (!picture.WebsiteSizesExist)
            {
                // optionally can create them on the fly, if needed.  Conceptually not a great place to do it,
                // but still useful when Fennica3 is run from KoTi for previews (the editor there will try to create
                // downsized versions when picture markup is inserted, but will miss if it's typed/pasted by hand).
                // Should be turned off in production still; this is the only place where Fennica3 might not be fully readonly.
                if (configuration["Fennica3:DownsizeImagesOnTheFly"] is not null)
                {
                    logger.LogInformation("Website sizes not found for picture #{id} ({filename}), creating", picture.Id, picture.Filename);
                    await pictureUpload.EnsureWebsiteVersionsExist(picture);
                }
                else
                {
                    logger.LogWarning("Website sizes not found for picture #{id} ({filename}), using original", picture.Id, picture.Filename);
                }
            }
            
            // set src and possibly srcset if downsized images exist
            if (picture.Website1xUrl != null)
            {
                image.SetAttributeValue("src", picture.Website1xUrl);
                if (picture.Website2xUrl != null)
                {
                    image.SetAttributeValue("srcset", $"{picture.Website1xUrl}, {picture.Website2xUrl} 2x");
                }
                else
                {
                    image.SetAttributeValue("srcset", $"{picture.Website1xUrl}, {picture.Url} 2x");
                }
            }

            // set proper sizes
            var (w, h) = picture.GetDownsizedDimensions(Picture.WebsiteSize);
            image.SetAttributeValue("width", w > 0 ? w : picture.Width);
            image.SetAttributeValue("height", h > 0 ? h : picture.Height);
            
            // always lazy load
            image.SetAttributeValue("loading", "lazy");

            // wrap into link to original image, if using downscaled image
            XElement imageOrLink = image;
            if (picture.Website1xUrl != null)
            {
                var link = new XElement("a");
                link.SetAttributeValue("href", picture.Url);
                image.ReplaceWith(link);
                link.Add(image);
                imageOrLink = link;
            }

            // wrap into <figure>, unless requested otherwise with custom attribute
            if (image.Attribute("nofigure") == null)
            {
                var figure = new XElement("figure");
                imageOrLink.ReplaceWith(figure);
                figure.Add(imageOrLink);
                
                // move alt text to <figcaption>
                var alt = image.Attribute("alt");
                if (alt != null && alt.Value.Length > 0)
                {
                    var figcaption = XElement.Parse($"<figcaption>{ExpandUrls(alt.Value)}</figcaption>");
                    figure.Add(figcaption);
                    // unset alt, since semantically it's likely not proper alt text anyway
                    // TODO: no, don't, breaks gallery captions, should fix galleries to look at figcaption instead
                    //image.SetAttributeValue("alt", "");
                }

                // unwrap from <p>
                if (figure.Parent != null && figure.Parent.Name == "p" && figure.Parent.Elements().Count() == 1)
                {
                   figure.Parent.ReplaceWith(figure);
                }
            }
        }
    }

    /// <summary>
    /// Automatically add ids on all heading elements.
    /// </summary>
    /// <param name="document">HTML as XHTML document</param>
    private void AddHeaderIds(XElement document)
    {
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
    }

    /// <summary>
    /// Wrap asides into a wrapper div.
    /// </summary>
    /// <param name="document">HTML as XHTML document</param>
    /// <exception cref="InvalidOperationException"></exception>
    private void WrapAsides(XElement document)
    {
        var asides = (from aside in document.Descendants("aside") select aside).ToList();
        foreach (var aside in asides)
        {
            if (aside.Attribute("class")?.Value != "wide")
            {
                var wrapper = new XElement("div");
                wrapper.SetAttributeValue("class", "aside-wrapper");
                wrapper.Add(aside);
                aside.ReplaceWith(wrapper);
            }
        }
    }

    /// <summary>
    /// Generate Glider gallery markup from images wrapped into special comments.
    /// </summary>
    /// <param name="html">HTML as string</param>
    /// <returns>Processed HTML</returns>
    private string FormatGalleries(string html)
    {
        // build HTML structure required by glider gallery library from
        // sequences of images wrapped in <!--gallery-->...<!--/gallery--> markup
        // rather hacky but works
        bool matched;
        int k = 1;
        const string openTag = "<!--gallery-->", closeTag = "<!--/gallery-->";  
            
        do
        {
            // find next <!--gallery-->...<!--/gallery--> block
            int openIndex = html.IndexOf(openTag, StringComparison.Ordinal);
            if (openIndex == -1)
            {
                break;
            }

            int closeIndex = html.IndexOf(closeTag, StringComparison.Ordinal);
            if (closeIndex == -1)
            {
                break;
            }

            matched = true;

            // parse block as XML
            string wrappedHtml = "<html>" + html[(openIndex + openTag.Length)..closeIndex] + "</html>";
            var subDocument = XElement.Parse(wrappedHtml, LoadOptions.PreserveWhitespace);

            // 2-4 images might not actually need a gallery, can be arranged in a row/grid.
            // But this requires some conditions, if any of them are not met, fall back to a gallery
            var images = subDocument.Descendants("img").ToList();
            bool needGallery = true;
            double aspectRatio = 0;
            if (images.Count <= 4)
            {
                needGallery = false;
                foreach (var image in images)
                {
                    // none of the images may have a caption
                    if (!String.IsNullOrEmpty(image.Attribute("alt")?.Value))
                    {
                        needGallery = true;
                        break;
                    }
                    
                    // width/height must be set (they normally would be)
                    if (!Int32.TryParse(image.Attribute("width")?.Value, out var w))
                    {
                        needGallery = true;
                        break;
                    }
                    
                    if (!Int32.TryParse(image.Attribute("height")?.Value, out var h))
                    {
                        needGallery = true;
                        break;
                    }
                    
                    if (aspectRatio == 0)
                    {
                        // remember aspect ratio of first image
                        // it must be >1.0, so the image must be horizontal
                        aspectRatio = 1.0 * w / h;
                        if (aspectRatio < 1.0)
                        {
                            needGallery = true;
                            break;
                        }
                    }
                    else
                    {
                        // for the rest of the images, aspect ratio must be the same as the first one
                        if (Math.Abs(1.0 * w / h - aspectRatio) > 0.01)
                        {
                            needGallery = true;
                            break;
                        }
                    }
                }
            }

            // build output HTML as regular string
            StringBuilder output = new StringBuilder();
            if (!needGallery)
            {
                // gallery-less
                output.Append($"<div class=\"multiple-images multiple-images-{images.Count}\">");
                foreach (var image in images)
                {
                    output.Append(image.Parent?.ToString(SaveOptions.DisableFormatting));
                }
                output.Append("</div>");
            }
            else
            {
                // glider gallery markup
                output.Append($"<div class=\"glider-contain\" data-id=\"{k}\"><div class=\"glider\">");
                // look only for images, any other content in gallery block will be ignored
                foreach (var image in from image in subDocument.Descendants("img") select image)
                {
                    output.Append("<div class=\"glider-image\">");
                    if (image.Parent != null && image.Parent!.Name == "a")
                    {
                        output.Append(
                            $"<a class=\"glider-external-link\" href=\"{image.Parent!.Attribute("href")!.Value}\"><i class=\"fas fa-external-link-alt\"></i></a>");
                    }

                    output.Append("<div class=\"glider-image-wrap\">");
                    // note: no width on purpose
                    output.Append(
                        $"<img loading=\"lazy\" src=\"{image.Attribute("src")!.Value}\" srcset=\"{(image.Attribute("srcset") != null ? image.Attribute("srcset")!.Value : "")}\" />");
                    if (image.Attribute("alt") != null && image.Attribute("alt")!.Value.Length > 0)
                    {
                        var p = XElement.Parse($"<p>{ExpandUrls(image.Attribute("alt")!.Value)}</p>");
                        output.Append(p);
                    }

                    output.Append("</div>"); // .glider-image-wrap
                    output.Append("</div>"); // .glider-image
                }

                output.Append("</div>"); // .glider
                output.Append(
                    $"<button id=\"glider-prev-{k}\" role=\"button\" aria-label=\"Previous\" class=\"glider-prev\"><i class=\"fa fa-chevron-left\"></i></button>");
                output.Append(
                    $"<button id=\"glider-next-{k}\" role=\"button\" aria-label=\"Next\" class=\"glider-next\"><i class=\"fa fa-chevron-right\"></i></button>");
                output.Append($"<div id=\"glider-dots-{k}\" class=\"glider-dots\"></div>");
                output.Append("</div>"); // .glider-contain
            }

            // replace original markup with generated
            html = html[..openIndex] + output + html[(closeIndex + closeTag.Length)..];
                
            // continue until no more blocks found
            k++;
        } while (matched);

        return html;
    }

    private string FormatPreliminary(string html)
    {
        html = html.Replace("<!--aside-->", "<aside>");
        html = html.Replace("<!--aside wide-->", "<aside class=\"wide\">");
        html = html.Replace("<!--/aside-->", "</aside>");
        return html;
    }
    
    /// <summary>
    /// Add some typography formatting to HTML so that it's nicer to read.
    /// </summary>
    /// <param name="html">HTML as string</param>
    /// <returns>Processed HTML</returns>
    private string FormatTypography(string html)
    {
        // convert double dashes into proper long dash (adding a nbsp before it)
        // this is probably not super reliable...  Take care to not destroy IDN URLs (with "xn--"!)
        html = Regex.Replace(html, "([>\\s])--([^-<>])", "$1—$2", RegexOptions.Singleline);
        html = Regex.Replace(html, "\\s—", "\u00a0—");
        
        // no break before various units
        html = Regex.Replace(html, "([.0-9]+)\\s+(с|сек|мин|ч|дн\\.|лет|год\\S*|век\\S*|шт\\.|м|км|см|мм|МВт|кВт|ГВт|кг|т|тонн\\S*|л|кв\\.|куб\\.|чел\\S*|тыс\\S*|млн\\.|млрд\\.)\\b", "$1\u00a0$2");
        html = Regex.Replace(html, "(кв\\.|куб\\.)\\s+", "$1\u00a0");
        
        // no break before numbers, including Roman ones
        html = Regex.Replace(html, " ([.0-9]+\\b)", "\u00a0$1");
        html = Regex.Replace(html, " ([IVXDCLM]+)", "\u00a0$1");
        
        return html;
    }
    
    /// <summary>
    /// Expands links into HTML hyperlinks inside of text or HTML.
    /// From: https://weblog.west-wind.com/posts/2006/Dec/21/Expanding-Urls-with-RegEx-in-NET
    /// </summary>
    /// <param name="input">The text to expand</param>
    public string ExpandUrls(string input)
    {
        string pattern = @"[""'=]?(http://|ftp://|https://|www\.|ftp\.[\w]+)([\w\-\.,@?^=%&amp;:/~\+#]*[\w\-\@?^=%&amp;/~\+#])";
        
        // *** Expand embedded hyperlinks
        RegexOptions options =
            RegexOptions.IgnorePatternWhitespace |
            RegexOptions.Multiline |
            RegexOptions.IgnoreCase;
        
        Regex reg = new Regex(pattern, options);

        return reg.Replace(input, match =>
        {
            string href = match.Value; // M.Groups[0].Value;
            
            // *** if string starts within an HREF don't expand it
            if (href.StartsWith("=") ||
                href.StartsWith("'") ||
                href.StartsWith("\""))
                return href;
            
            href = href.Replace("&", "&amp;");

            string text = href;
            if (href.IndexOf("://") < 0)
            {
                if (href.StartsWith("www."))
                    href = "http://" + href;
                else if (href.StartsWith("ftp"))
                    href = "ftp://" + href;
                else if (href.IndexOf("@") > -1)
                    href = "mailto:" + href;
            }
            
            return $"<a href=\"{href}\">{text}</a>";
        });
    }


}