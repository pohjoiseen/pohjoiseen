using Microsoft.AspNetCore.Http;

namespace Teos;

/// <summary>
/// Development server for Teos.  This just renders content whenever the URL matches.  
/// </summary>
public class Server
{
    private readonly RequestDelegate _next;
    private readonly TeosEngine _engine;
    private readonly string _defaultRedirect;

    /// <summary>
    /// Server constructor.
    /// </summary>
    /// <param name="next"></param>
    /// <param name="engine">TeosEngine to use</param>
    /// <param name="defaultRedirect">Page to which default / URL should redirect</param>
    public Server(RequestDelegate next, TeosEngine engine, string defaultRedirect = null)
    {
        _next = next;
        _engine = engine;
        _defaultRedirect = defaultRedirect;
    }

    /// <summary>
    /// Server middleware.
    /// </summary>
    /// <param name="httpContext"></param>
    public async Task InvokeAsync(HttpContext httpContext)
    {
        // default page can optionally redirect (normal HTTP redirect) somewhere else
        if (httpContext.Request.Path == "/" && _defaultRedirect != null)
        {
            httpContext.Response.Redirect(_defaultRedirect);
            return;
        }
            
        // render with TeosEngine and continue if not matched
        bool handled = await _engine.TryRender(httpContext.Request.Path, httpContext.Response);
        if (!handled)
        {
            await _next(httpContext);
        }
    }
}
