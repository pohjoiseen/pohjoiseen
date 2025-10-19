using System.Xml.Linq;
using SixLabors.ImageSharp;

namespace Teos;

/// <summary>
/// Default formatter that converts references to content items in links and images sources
/// to canonical URLs, and for images additionally set correct src/srcset in accordance with image sizes
/// and wrap into links to original image and figure tag.
/// 
/// This is fairly messy and tailored for Fennica project needs, but still generalpurpose-ish enough.
/// </summary>
public class LinkAndImageFormatter : IContentFormatter
{
    private readonly IList<string> _convertLinksTo;
    private readonly IList<(string, string)> _imageSizes;
    private readonly bool _linkOriginalImage;
    private readonly bool _wrapImagesInFigure;

    private ITeosEngine _teosEngine;
    
    public void SetTeosEngine(ITeosEngine teosEngine)
    {
        _teosEngine = teosEngine;
    }

    /// <summary>
    /// LinkAndImageFormatter constructor with settings.
    /// </summary>
    /// <param name="convertLinksTo">List of extensions (".md", ...) of source content files to process links to.  All other
    ///     links will be left alone</param>
    /// <param name="imageSizes">List of target image sizes; if non-empty, will replace images with downsized auto-generated
    ///     versions.  Tuples of (filename suffix, srcset suffix), in ascending order, e. g. ("normal", ""), ("retina", "2x").
    ///     Should be configured same way as with ImageProcessor</param>
    /// <param name="linkOriginalImage">Wrap images into links to original full-size image, if replaced with reduced versions</param>
    /// <param name="wrapImagesInFigure">Wrap images with figure tags, moving alt text into figcaption if present</param>
    public LinkAndImageFormatter(
        IList<string> convertLinksTo,
        IList<(string, string)> imageSizes,
        bool linkOriginalImage = false,
        bool wrapImagesInFigure = false)
    {
        _convertLinksTo = convertLinksTo;
        _imageSizes = imageSizes;
        _linkOriginalImage = linkOriginalImage;
        _wrapImagesInFigure = wrapImagesInFigure;
    }

    /// <inheritdoc cref="IContentFormatter.FormatHTML"/>
    public async Task<string> FormatHTML(string html, string path)
    {
        // use LINQ to XML to manipulate HTML
        // Markdown parser generates XHTML-compatible HTML (although it is possible
        // to have also manually added HTML which would be malformed as XML)
        // We just need to temporarily wrap this XHTML so that there is one root tag
        var wrappedHtml = "<html>" + html + "</html>";
        var document = XElement.Parse(wrappedHtml, LoadOptions.PreserveWhitespace);

        // only interested in base dir
        convertAHref(document, path);
        await convertImgSrc(document, path);

        // back to string and unwrap
        wrappedHtml = document.ToString(SaveOptions.DisableFormatting);
        return wrappedHtml.Replace("<html>", "").Replace("</html>", "");
    }

    private XElement convertAHref(XElement document, string source)
    {
        // shortcut if nothing to do
        if (_convertLinksTo.Count == 0)
        {
            return document;
        }

        // get all <a> in a document
        foreach (var link in from link in document.Descendants("a") select link)
        {
            // look for href attribute
            var href = link.Attribute("href");
            if (href == null)
            {
                continue;
            }

            // without host specified or only hash
            if (href.Value.Contains("://") || href.Value.StartsWith('#'))
            {
                continue;
            }

            // which contain specified patterns
            var found = false;
            foreach (var pattern in _convertLinksTo)
            {
                if (href.Value.EndsWith(pattern) || href.Value.Contains(pattern + "#"))
                { 
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                continue;
            }

            // convert from relative path if necessary
            var path = _teosEngine.ResolvePath(href.Value, source);

            // possible hash in link, remove and preserve it
            var hash = "";
            var hashIndex = path.IndexOf('#');
            if (hashIndex != -1)
            {
                hash = path.Substring(hashIndex + 1);
                path = path.Substring(0, hashIndex);
            }

            (Content, IContentController) contentTuple;
            if (_teosEngine.AllContent.TryGetValue(path, out contentTuple))
            {
                // replace with canonical content URL
                if (hash.Length > 0)
                {
                    link.SetAttributeValue("href", contentTuple.Item1.CanonicalURL + "#" + hash);
                }
                else
                {
                    link.SetAttributeValue("href", contentTuple.Item1.CanonicalURL);
                }
            }
            else
            {
                // content must exist
                throw new Exception("Could not resolve content item: " + href.Value);
            }
        }
        return document;
    }

    private async Task<XElement> convertImgSrc(XElement document, string source)
    {
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

            // without host specified
            if (src.Value.Contains("://"))
            {
                continue;
            }

            // convert from relative path if necessary
            var path = _teosEngine.ResolvePath(src.Value, source);

            // found image
            string baseSrc = path[0] == '/' ? path : "/" + path;
            image.SetAttributeValue("src", baseSrc);

            // now look for files in various sizes
            List<string> srcSet = new List<string>();
            foreach (var size in _imageSizes)
            {
                string suffix = size.Item1, srcSetValue = size.Item2;
                string srcForSize = GenerateImageFilename(baseSrc, suffix);
                string filenameForSize = _teosEngine.BuildPath + srcForSize;
                // ImageProcessor does not upscale images, so not every image might actually exist
                if (File.Exists(filenameForSize))
                {
                    if (srcSetValue.Length > 0)
                    {
                        // "2x" or different scaled image, add to srcset
                        srcSet.Add(srcForSize + " " + srcSetValue);
                    }
                    else
                    {
                        // 1x size, this should also be set as default src
                        srcSet.Add(srcForSize);
                        image.SetAttributeValue("src", srcForSize);
                        // and set width/height from this image, determine from actual image file
                        var imageInfo = await Image.IdentifyAsync(filenameForSize);
                        image.SetAttributeValue("width", imageInfo.Size.Width);
                        image.SetAttributeValue("height", imageInfo.Size.Height);
                    }
                }
            }
            
            // XXX If only one size found, force full-size image as a 2x option
            // Seems like not the right way to fix this but I'm tired of too small pictures suddenly not having a retina option...
            if (srcSet.Count == 1)
            {
                srcSet.Add(baseSrc + " 2x");
            }
            
            // set srcset if there is more than one valid size
            if (srcSet.Count > 1)
            {
                image.SetAttributeValue("srcset", string.Join(", ", srcSet));
            }

            // wrap into <figure>, if this has not been disabled with nofigure non-standard attribute and if this setting is set
            if (_wrapImagesInFigure && image.Attribute("nofigure") == null)
            {
                var figure = new XElement("figure");
                image.ReplaceWith(figure);
                figure.Add(image);
                var alt = image.Attribute("alt");
                if (alt != null && alt.Value.Length > 0)
                {
                    var figcaption = new XElement("figcaption");
                    figcaption.SetValue(alt.Value);
                    figure.Add(figcaption);
                }

                // TODO: unwrap from <p>
                // This leads to null reference crashes in the outermost foreach from...in,
                // presumably because the document structure is changed too much
                // if (figure.Parent != null && figure.Parent.Name == "p" && figure.Parent.Elements().Count() == 1)
                // {
                //    figure.Parent.ReplaceWith(figure);
                // }
            }
            
            // always lazy load (FIXME: should be configurable, can't be bothered right now...)
            image.SetAttributeValue("loading", "lazy");

            // wrap into <a>, if the image is no longer the original one and if this setting is set
            if (_linkOriginalImage && image.Attribute("src")!.Value != baseSrc)
            {
                var link = new XElement("a");
                link.SetAttributeValue("href", baseSrc);
                image.ReplaceWith(link);
                link.Add(image);
            }
        }

        return document;
    }
    private string GenerateImageFilename(string path, string suffix)
    {
        if (suffix.Length > 0)
        {
            var indexOfLastDot = path.LastIndexOf(".");
            return path.Substring(0, indexOfLastDot) + "." + suffix + "." + path.Substring(indexOfLastDot + 1);
        }
        return path;
    }
}