using Holvi;
using Holvi.Models;
using KoTi.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace KoTi.Controllers.App;

public class PostListViewComponent(HolviDbContext dbContext, IMemoryCache memoryCache) : ViewComponent
{
    public static readonly int DefaultLimit = 25;

    public async Task<IViewComponentResult> InvokeAsync(
        string componentId,
        string language,
        int limit,
        int offset,
        string? postSearch)
    {
        // cache postSearch and offset per componentId
        // allow using cached values if special parameters are passed
        if (postSearch == "#")
        {
            if (!memoryCache.TryGetValue($"{componentId}:{language}:postSearch", out postSearch))
            {
                postSearch = null;
            }
        }

        if (offset == -1)
        {
            if (!memoryCache.TryGetValue($"{componentId}:{language}:offset", out offset))
            {
                offset = 0;
            }
        }
        
        memoryCache.Set($"{componentId}:{language}:postSearch", postSearch, TimeSpan.FromDays(7));
        memoryCache.Set($"{componentId}:{language}:offset", offset, TimeSpan.FromDays(7));
        
        // query for posts
        var query = dbContext.Posts
            .Include(p => p.TitlePicture)
            .Where(p => p.Language == language)
            .AsQueryable();
        if (postSearch != null)
        {
            query = query.Where(p => p.Name.ToLower().Contains(postSearch.ToLower()) || p.Title.ToLower().Contains(postSearch.ToLower()));
        }
        query = query.OrderByDescending(p => p.Date).ThenByDescending(p => p.Name);

        var queryPaginated = query;
        if (offset > 0)
        {
            queryPaginated = queryPaginated.Skip(offset);
        }
        if (limit > 0)
        {
            queryPaginated = queryPaginated.Take(limit);
        }
        else
        {
            queryPaginated = queryPaginated.Take(DefaultLimit);
        }
        
        return View("~/Views/Posts/_List.cshtml", new PostListViewModel
        {
            ComponentId = componentId,
            Language = language,
            Total = await query.CountAsync(),
            Limit = limit > 0 ? limit : DefaultLimit,
            Offset = offset,
            Posts = await queryPaginated.ToListAsync(),
            PostSearchQuery = postSearch
        });
    }
}