using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

namespace Teos;

/// <summary>
/// Renders, and also formats and defines routes, a particular content item.
/// TODO: can this use a type parameter?
/// </summary>
public interface IContentController : ITeosEngineAware
{
    /// <summary>
    /// Apply HTML formatting (with IFormatHTML passed with SetParameters()) to a particular content item.
    /// The default HTML property and all possibly other HTML snippets should be formatted.
    /// </summary>
    /// <param name="content">Content item to process</param>
    /// <returns>Async task</returns>
    public Task ApplyFormatting(Content content)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the list of routes (regexps) which this controller should match for a particular content item.
    /// </summary>
    /// <param name="content">Content item to match</param>
    /// <returns>List of regexps</returns>
    public IList<string> GetRoutes(Content content);

    /// <summary>
    /// Gets the list of exact URLs which this controller should generate for a particular content item.
    /// TODO: merge with GetRoutes()?
    /// </summary>
    /// <param name="content">Content item to match</param>
    /// <returns>List of URLs</returns>
    public IList<string> GetURLs(Content content);

    /// <summary>
    /// Render a content item into HttpResponse.  Usually this should render an HTML page, but could be JSON
    /// or anything else if necessary.
    /// </summary>
    /// <param name="content">Content item to render</param>
    /// <param name="routeMatch">Parameters with which a route from GetRoutes() was matched</param>
    /// <param name="response">Response to render to</param>
    /// <returns>Async task</returns>
    public Task Render(Content content, GroupCollection routeMatch, HttpResponse response);
}