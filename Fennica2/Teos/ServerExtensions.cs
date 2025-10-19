using Microsoft.AspNetCore.Builder;

namespace Teos;

/// <summary>
/// Extension method used to add Teos dev server middleware to the HTTP request pipeline.
/// </summary>
public static class ServerExtensions
{
    public static IApplicationBuilder UseTeosServer(this IApplicationBuilder builder, TeosEngine engine, string defaultRedirect = null)
    {
        return builder.UseMiddleware<Server>(engine, defaultRedirect);
    }
}
