using System.Text.RegularExpressions;
using Holvi;
using Holvi.Models;
using KoTi.ViewModels.Redirects;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KoTi.Controllers.App.Redirects;

public partial class RedirectsController(HolviDbContext dbContext) : Controller
{
    private const int Limit = 50;
    
    [GeneratedRegex("^(post|article):([0-9]+)(#.*)?$")]
    private static partial Regex ContentTargetRegex();

    [HttpGet("/redirects")]
    public async Task<IActionResult> Index([FromQuery] int offset)
    {
        return View(await GetList(offset));
    }

    [HttpDelete("/redirects/{id:int}")]
    public async Task<IActionResult> Delete(int id, [FromQuery] int offset)
    {
        var redirect = await dbContext.Redirects.FindAsync(id);
        if (redirect is not null)
        {
            dbContext.Redirects.Remove(redirect);
            await dbContext.SaveChangesAsync();
        }

        return View("_List", await GetList(offset));
    }

    [HttpGet("/redirects/add/{kind:regex(^(url|post|article)$)}")]
    public IActionResult Add(string kind, [FromQuery] string? language)
    {
        return View(new RedirectAddViewModel
        {
            Kind = kind,
            Language = language is not null && Fennica3.Fennica3.Languages.Contains(language) ? language : Fennica3.Fennica3.Languages[0]
        });
    }

    [HttpPost("/redirects/add/{kind:regex(^(url|post|article)$)}")]
    public async Task<IActionResult> Add(string kind, [FromForm] string? urlFrom, [FromForm] string? urlTo, [FromForm] string? anchor)
    {
        urlFrom = urlFrom?.Trim() ?? "";
        urlTo = urlTo?.Trim() ?? "";
        anchor = anchor?.Trim().TrimStart('#') ?? "";

        // allow pasting a full URL as source, but we only match paths
        if (Uri.TryCreate(urlFrom, UriKind.Absolute, out var uriFrom) && uriFrom.Scheme.StartsWith("http"))
        {
            urlFrom = Uri.UnescapeDataString(uriFrom.AbsolutePath);
        }
        if (!urlFrom.StartsWith('/'))
        {
            return Error("Source must be a path starting with /");
        }

        if (kind == "url")
        {
            if (urlTo.Length == 0)
            {
                return Error("Target URL is required");
            }
        }
        else
        {
            var match = ContentTargetRegex().Match(urlTo);
            if (!match.Success || match.Groups[1].Value != kind || match.Groups[3].Success)
            {
                return Error($"Please select {(kind == "post" ? "a post" : "an article")}");
            }

            var id = Int32.Parse(match.Groups[2].Value);
            if (kind == "post" ? !await dbContext.Posts.AnyAsync(p => p.Id == id) : !await dbContext.Articles.AnyAsync(a => a.Id == id))
            {
                return Error($"Selected {kind} does not exist");
            }

            if (anchor.Length > 0)
            {
                urlTo += "#" + anchor;
            }
        }

        // Fennica3 disregards trailing slash when looking up redirects
        var urlFromNoSlash = urlFrom.TrimEnd('/');
        if (await dbContext.Redirects.AnyAsync(r => r.UrlFrom == urlFromNoSlash || r.UrlFrom == urlFromNoSlash + "/"))
        {
            return Error("Redirect already exists");
        }

        dbContext.Redirects.Add(new Redirect { UrlFrom = urlFrom, UrlTo = urlTo });
        await dbContext.SaveChangesAsync();

        Response.Headers.Append("HX-Redirect", Url.Action("Index"));
        return NoContent();
    }

    private IActionResult Error(string message)
    {
        Response.StatusCode = StatusCodes.Status400BadRequest;
        return View("_Error", message);
    }

    private async Task<RedirectListViewModel> GetList(int offset)
    {
        var total = await dbContext.Redirects.CountAsync();
        // can happen after deleting the last redirect on the last page
        if (offset >= total)
        {
            offset = Math.Max(0, (total - 1) / Limit * Limit);
        }

        var redirects = await dbContext.Redirects
            .OrderBy(r => r.UrlFrom)
            .Skip(offset)
            .Take(Limit)
            .ToListAsync();

        // resolve post:XXX/article:XXX targets all at once
        var items = redirects
            .Select(r => new { Redirect = r, Match = ContentTargetRegex().Match(r.UrlTo) })
            .ToList();
        var postIds = items.Where(i => i.Match.Success && i.Match.Groups[1].Value == "post")
            .Select(i => Int32.Parse(i.Match.Groups[2].Value)).ToList();
        var articleIds = items.Where(i => i.Match.Success && i.Match.Groups[1].Value == "article")
            .Select(i => Int32.Parse(i.Match.Groups[2].Value)).ToList();
        var posts = await dbContext.Posts.Where(p => postIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Title, p.Date, p.Language }).ToDictionaryAsync(p => p.Id);
        var articles = await dbContext.Articles.Where(a => articleIds.Contains(a.Id))
            .Select(a => new { a.Id, a.Title, a.Language }).ToDictionaryAsync(a => a.Id);

        return new RedirectListViewModel
        {
            Total = total,
            Limit = Limit,
            Offset = offset,
            Redirects = items.Select(i =>
            {
                var item = new RedirectItemViewModel { Redirect = i.Redirect };
                if (!i.Match.Success)
                {
                    return item;
                }

                var id = Int32.Parse(i.Match.Groups[2].Value);
                item.TargetHash = i.Match.Groups[3].Value;
                if (i.Match.Groups[1].Value == "post")
                {
                    item.TargetKind = "Post";
                    if (posts.TryGetValue(id, out var post))
                    {
                        item.TargetTitle = $"{post.Title} ({post.Date:yyyy-MM-dd})";
                        item.TargetEditUrl = Url.Action("Edit", "Posts", new { id, language = post.Language });
                    }
                }
                else
                {
                    item.TargetKind = "Article";
                    if (articles.TryGetValue(id, out var article))
                    {
                        item.TargetTitle = article.Title;
                        item.TargetEditUrl = Url.Action("Edit", "Articles", new { id, language = article.Language });
                    }
                }

                return item;
            }).ToList()
        };
    }
}
