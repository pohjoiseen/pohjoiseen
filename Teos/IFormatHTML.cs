namespace Teos;

/// <summary>
/// Holds the HTML formatter function, with the formatter chain applied.
/// </summary>
public interface IFormatHTML
{
    /// <summary>
    /// Runs the formatter chain on a fragment of HTML.
    /// </summary>
    /// <param name="html">HTML to postprocess</param>
    /// <param name="path">Path to original content item for which HTML is being formatted</param>
    /// <returns>Postprocessed HTML, async</returns>
    public Task<string> FormatHTML(string html, string path);
}

