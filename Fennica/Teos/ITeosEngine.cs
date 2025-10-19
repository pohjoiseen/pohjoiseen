namespace Teos;

/// <summary>
/// TeosEngine interface, for use in controllers, formatters etc.
/// </summary>
public interface ITeosEngine
{
    /// <summary>
    /// Absolute path to content source dir.
    /// </summary>
    public string ContentPath { get; }
    
    /// <summary>
    /// Absolute path to output dir.
    /// </summary>
    public string BuildPath { get; }
    
    /// <summary>
    /// Currently known static files (path => processor).
    /// </summary>
    public IDictionary<string, IStaticProcessor> StaticFiles { get; }
    
    /// <summary>
    /// Currently loaded content files (path => content itself, controller).
    /// </summary>
    public IDictionary<string, (Content, IContentController)> AllContent { get; }
    
    /// <summary>
    /// Runs the formatter chain on a fragment of HTML.
    /// </summary>
    /// <param name="html">HTML to postprocess</param>
    /// <param name="path">Path to original content item for which HTML is being formatted</param>
    /// <returns>Postprocessed HTML, async</returns>
    public Task<string> FormatHTML(string html, string path);

    /// <summary>
    /// Resolves relative path to "target" content or static file, from "source" file.  
    /// </summary>
    /// <param name="target">Target content or static file</param>
    /// <param name="source">Source content file</param>
    /// <returns></returns>
    public string ResolvePath(string target, string source);
}