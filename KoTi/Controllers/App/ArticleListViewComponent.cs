using Holvi;
using KoTi.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace KoTi.Controllers.App;

public class ArticleListViewComponent(HolviDbContext dbContext, IMemoryCache memoryCache) : ViewComponent
{
    public static readonly int DefaultLimit = 25;

    public async Task<IViewComponentResult> InvokeAsync(
        string componentId,
        string language,
        int limit,
        int offset,
        string? articleSearch,
        bool? linkOnly = false)
    {
        // cache articleSearch and offset per componentId
        // allow using cached values if special parameters are passed
        if (articleSearch == "#")
        {
            if (!memoryCache.TryGetValue($"{componentId}:{language}:articleSearch", out articleSearch))
            {
                articleSearch = null;
            }
        }

        if (offset == -1)
        {
            if (!memoryCache.TryGetValue($"{componentId}:{language}:offset", out offset))
            {
                offset = 0;
            }
        }
        
        memoryCache.Set($"{componentId}:{language}:articleSearch", articleSearch, TimeSpan.FromDays(7));
        memoryCache.Set($"{componentId}:{language}:offset", offset, TimeSpan.FromDays(7));
        
        // query for articles
        var query = dbContext.Articles
            .Where(a => a.Language == language)
            .AsQueryable();
        if (articleSearch != null)
        {
            query = query.Where(a => a.Name.ToLower().Contains(articleSearch.ToLower()) || a.Title.ToLower().Contains(articleSearch.ToLower()));
        }
        query = query.OrderBy(a => a.Name);

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
        
        return View("~/Views/Articles/_List.cshtml", new ArticleListViewModel
        {
            ComponentId = componentId,
            Language = language,
            Total = await query.CountAsync(),
            Limit = limit > 0 ? limit : DefaultLimit,
            Offset = offset,
            Articles = await queryPaginated.ToListAsync(),
            ArticleSearchQuery = articleSearch,
            LinkOnly = linkOnly ?? false
        });
    }
}