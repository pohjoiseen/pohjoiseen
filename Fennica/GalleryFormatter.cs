using System.Text;
using System.Xml.Linq;
using Teos;

namespace Fennica;

/// <summary>
/// Converts pseudo-markup <!--gallery-->...<!--/gallery--> into markup usable by glider image carousel JS library.
/// 
/// Expects HTML:
/// <!--gallery-->
/// <img src="image1.jpg" />
/// <img src="image2.jpg" />
/// ...
/// <!--/gallery-->
///
/// Will replace every such block by appropriate markup.  Only img tags and a tags wrapping them are used, everything
/// else is discarded completely.  src, srcset and alt attributes of img tags, and href element of a tags are used.
/// Images of course should probably already be processed by LinkAndImageFormatter.
/// </summary>
public class GalleryFormatter : IContentFormatter
{ 
    /// <inheritdoc cref="IContentFormatter.FormatHTML"/>
    public Task<string> FormatHTML(string html, string path)
    {
        bool matched;
        int k = 1;
        const string openTag = "<!--gallery-->", closeTag = "<!--/gallery-->";  
            
        do
        {
            // find next <!--gallery-->...<!--/gallery--> block
            int openIndex = html.IndexOf(openTag);
            if (openIndex == -1)
            {
                break;
            }

            int closeIndex = html.IndexOf(closeTag);
            if (closeIndex == -1)
            {
                break;
            }

            matched = true;

            // parse block as XML
            string wrappedHtml = "<html>" + html[(openIndex + openTag.Length)..closeIndex] + "</html>";
            var subDocument = XElement.Parse(wrappedHtml, LoadOptions.PreserveWhitespace);
                
            // build output HTML as regular string
            StringBuilder output =
                new StringBuilder($"<div class=\"glider-contain\" data-id=\"{k}\"><div class=\"glider\">");
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
                output.Append($"<img loading=\"lazy\" src=\"{image.Attribute("src")!.Value}\" srcset=\"{(image.Attribute("srcset") != null ? image.Attribute("srcset")!.Value : "")}\" />");
                if (image.Attribute("alt") != null && image.Attribute("alt")!.Value.Length > 0)
                {
                    output.Append("<p>" + new XText(image.Attribute("alt")!.Value) + "</p>");
                }
                output.Append("</div>");  // .glider-image-wrap
                output.Append("</div>");  // .glider-image
            }
            output.Append("</div>");  // .glider
            output.Append($"<button id=\"glider-prev-{k}\" role=\"button\" aria-label=\"Previous\" class=\"glider-prev\"><i class=\"fa fa-chevron-left\"></i></button>");
            output.Append($"<button id=\"glider-next-{k}\" role=\"button\" aria-label=\"Next\" class=\"glider-next\"><i class=\"fa fa-chevron-right\"></i></button>");
            output.Append($"<div id=\"glider-dots-{k}\" class=\"glider-dots\"></div>");
            output.Append("</div>");  // .glider-contain

            // replace original markup with generated
            html = html[..openIndex] + output + html[(closeIndex + closeTag.Length)..];
                
            // continue until no more blocks found
            k++;
        } while (matched);

        return Task.FromResult(html);
    }
}