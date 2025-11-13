using System.Text.RegularExpressions;
using Holvi;
using Microsoft.EntityFrameworkCore;

namespace Fennica3.Middleware;

public class RedirectMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, HolviDbContext dbContext, Helpers helpers)
    {
        var redirect = await dbContext.Redirects.FirstOrDefaultAsync(r => r.UrlFrom == context.Request.Path.Value ||
            r.UrlFrom + "/" == context.Request.Path.Value ||
            r.UrlFrom == context.Request.Path.Value + "/");
        if (redirect is not null)
        {
            var url = redirect.UrlTo;
            
            if (url.StartsWith("post:"))
            {
                var match = Regex.Match(url, "post:([0-9]+)(#.*)?");
                if (match.Success)
                {
                    var postId = Int32.Parse(match.Groups[1].Value);
                    var hash = match.Length > 1 ? match.Groups[2].Value : "";
                    var post = await dbContext.Posts.FindAsync(postId);
                    if (post is not null)
                    {
                        url = helpers.PostLink(post) + hash;
                    }
                }
            }
            if (url.StartsWith("article:"))
            {
                var match = Regex.Match(url, "article:([0-9]+)(#.*)?");
                if (match.Success)
                {
                    var articleId = Int32.Parse(match.Groups[1].Value);
                    var hash = match.Length > 1 ? match.Groups[2].Value : "";
                    var article = await dbContext.Articles.FindAsync(articleId);
                    if (article is not null)
                    {
                        url = helpers.ArticleLink(article) + hash;
                    }
                }
            }

            context.Response.Redirect(url);
            return;
        }
        
        await next(context);
    }
}

public static class RedirectMiddlewareExtensions
{
    public static IApplicationBuilder UseRedirectMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RedirectMiddleware>();
    }
}