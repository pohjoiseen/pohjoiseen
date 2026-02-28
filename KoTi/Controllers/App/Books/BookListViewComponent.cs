using Holvi;
using KoTi.ViewModels.Books;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace KoTi.Controllers.App.Books;

public class BookListViewComponent(HolviDbContext dbContext, IMemoryCache memoryCache) : ViewComponent
{
    public static readonly int DefaultLimit = 25;

    public async Task<IViewComponentResult> InvokeAsync(
        string componentId,
        string language,
        int limit,
        int offset,
        string? bookSearch,
        bool? linkOnly = false)
    {
        // cache bookSearch and offset per componentId
        // allow using cached values if special parameters are passed
        if (bookSearch == "#")
        {
            if (!memoryCache.TryGetValue($"{componentId}:{language}:bookSearch", out bookSearch))
            {
                bookSearch = null;
            }
        }

        if (offset == -1)
        {
            if (!memoryCache.TryGetValue($"{componentId}:{language}:offset", out offset))
            {
                offset = 0;
            }
        }
        
        memoryCache.Set($"{componentId}:{language}:bookSearch", bookSearch, TimeSpan.FromDays(7));
        memoryCache.Set($"{componentId}:{language}:offset", offset, TimeSpan.FromDays(7));
        
        // query for books
        var query = dbContext.Books
            .Where(a => a.Language == language)
            .AsQueryable();
        if (bookSearch != null)
        {
            query = query.Where(a => a.Name.ToLower().Contains(bookSearch.ToLower()) || a.Title.ToLower().Contains(bookSearch.ToLower()));
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
        
        return View("~/Views/Books/_List.cshtml", new BookListViewModel
        {
            ComponentId = componentId,
            Language = language,
            Total = await query.CountAsync(),
            Limit = limit > 0 ? limit : DefaultLimit,
            Offset = offset,
            Books = await queryPaginated.ToListAsync(),
            BookSearchQuery = bookSearch,
            LinkOnly = linkOnly ?? false
        });
    }
}