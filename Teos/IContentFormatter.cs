namespace Teos;

/// <summary>
/// Applies some postprocessing to an arbitrary HTML snippet.  There may be many formatters, and all formatters
/// will be applied to all HTML in the project. 
/// </summary>
public interface IContentFormatter : ITeosEngineAware
{
    /// <summary>
    /// Runs the formatter on a fragment of HTML.
    /// </summary>
    /// <param name="html">HTML to postprocess</param>
    /// <param name="path">Path to original content item for which HTML is being formatted</param>
    /// <returns>Postprocessed HTML, async</returns>
    public Task<string> FormatHTML(string html, string path);
}