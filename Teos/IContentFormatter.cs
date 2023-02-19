namespace Teos;

/// <summary>
/// Applies some postprocessing to an arbitrary HTML snippet.  There may be many formatters, and all formatters
/// will be applied to all HTML in the project. 
/// </summary>
public interface IContentFormatter
{
    /// <summary>
    /// Sets various parameters which can be useful for the formatter.
    /// </summary>
    /// <param name="contentPath">Absolute path to content dir</param>
    /// <param name="buildPath">Absolute path to output dir</param>
    /// <param name="staticFiles">Full static files map</param>
    /// <param name="allContent">Full content map</param>
    public void SetParameters(string contentPath, string buildPath,
        IDictionary<string, IStaticProcessor> staticFiles,
        IDictionary<string, (Content, IContentController)> allContent)
    {
    }

    /// <summary>
    /// Runs the formatter on a fragment of HTML.
    /// </summary>
    /// <param name="html">HTML to postprocess</param>
    /// <param name="path">Path to original content item for which HTML is being formatted</param>
    /// <returns>Postprocessed HTML, async</returns>
    public Task<string> FormatHTML(string html, string path);
}