using Holvi;
using KoTi.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace KoTi.Controllers.App;

public class ArticlesPickerViewComponent(HolviDbContext dbContext, IMemoryCache memoryCache) : ViewComponent
{
    public static readonly int DefaultLimit = 25;

    public async Task<IViewComponentResult> InvokeAsync(
        string componentId,
        int limit,
        int offset,
        string? articleSearch)
    {
        // cache articleSearch and offset per componentId
        // allow using cached values if special parameters are passed
        if (articleSearch == "#")
        {
            if (!memoryCache.TryGetValue($"{componentId}:articleSearch", out articleSearch))
            {
                articleSearch = null;
            }
        }

        if (offset == -1)
        {
            if (!memoryCache.TryGetValue($"{componentId}:offset", out offset))
            {
                offset = 0;
            }
        }
        
        memoryCache.Set($"{componentId}:articleSearch", articleSearch, TimeSpan.FromDays(7));
        memoryCache.Set($"{componentId}:offset", offset, TimeSpan.FromDays(7));
        
        // query for articles
        var query = dbContext.Articles.AsQueryable();
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
        
        return View("~/Views/Shared/_ArticlePicker.cshtml", new ArticlePickerViewModel
        {
            ComponentId = componentId,
            Total = await query.CountAsync(),
            Limit = limit > 0 ? limit : DefaultLimit,
            Offset = offset,
            Articles = await queryPaginated.ToListAsync(),
            ArticleSearchQuery = articleSearch
        });
    }
}