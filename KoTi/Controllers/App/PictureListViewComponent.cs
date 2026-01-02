using Holvi;
using Holvi.Models;
using KoTi.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace KoTi.Controllers.App;

public class PictureListViewComponent(HolviDbContext dbContext, IMemoryCache memoryCache) : ViewComponent
{
    public static readonly int DefaultLimit = 100;

    public async Task<IViewComponentResult> InvokeAsync(
        string componentId,
        int limit,
        int offset,
        int? setId,
        string? setSearch)
    {
        // cache last setId per component, and setSearch and offset per setId;
        // allow using cached values if special parameters are passed
        if (setId == -1)
        {
            if (!memoryCache.TryGetValue($"{componentId}:setId", out setId))
            {
                setId = 0;
            }
        }

        if (setSearch == "#")
        {
            if (!memoryCache.TryGetValue($"{componentId}:setId:{setId}:setSearch", out setSearch))
            {
                setSearch = null;
            }
        }

        if (offset == -1)
        {
            if (!memoryCache.TryGetValue($"{componentId}:setId:{setId}:offset", out offset))
            {
                offset = 0;
            }
        }
        
        memoryCache.Set($"{componentId}:setId", setId, TimeSpan.FromDays(7));
        memoryCache.Set($"{componentId}:setId:{setId}:setSearch", setSearch, TimeSpan.FromDays(7));
        memoryCache.Set($"{componentId}:setId:{setId}:offset", offset, TimeSpan.FromDays(7));
        
        // query for pictures, allow all kinds of filtering
        var query = dbContext.Pictures
            .Include(p => p.Set)
            .AsQueryable();

        query = query.OrderBy(p => p.PhotographedAt);
        if (setId != null)
        {
            if (setId > 0)
            {
                query = query.Where(p => p.SetId == setId);
            }
            else
            {
                query = query.Where(p => p.SetId == null);
            }
        }

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

        PictureSet? pictureSet = null;
        if (setId != null)
        {
            if (setId > 0)
            {
                pictureSet = await dbContext.PictureSets
                    .Include(ps => ps.Children)
                    .Where(ps => ps.Id == setId)
                    .OrderBy(ps => ps.Name)
                    .FirstOrDefaultAsync();
                if (pictureSet == null)
                {
                    throw new BadHttpRequestException("Picture set not found");
                }
            }
            else
            {
                pictureSet = new PictureSet
                {
                    Id = 0,
                    Name = "All pictures",
                    Children = await dbContext.PictureSets
                        .Where(ps => ps.ParentId == null)
                        .OrderBy(ps => ps.Name)
                        .ToListAsync()
                };
            }
        }

        return View("~/Views/Pictures/_List.cshtml", new PictureListViewModel
        {
            ComponentId = componentId,
            Total = await query.CountAsync(),
            Limit = limit > 0 ? limit : DefaultLimit,
            Offset = offset,
            Pictures = await queryPaginated.ToListAsync(),
            PictureSet = pictureSet,
            PictureSetSearchQuery = setSearch,
            ChildrenPictureSetThumbnails = pictureSet != null
                ? await GetPictureSetsThumbnails(pictureSet.Children)
                : new Dictionary<int, IEnumerable<string>>(),
        });
    }

    private async Task<IDictionary<int, IEnumerable<string>>> GetPictureSetsThumbnails(
        IEnumerable<PictureSet> pictureSets)
    {
        // N+1 queries, but should be alright with SQLite?  It still is quite slow in practice...
        // otherwise would need to drop into fully raw SQL
        // with something like:
        // SELECT ps.*, ps.Name, GROUP_CONCAT(x.ThumbnailUrl)
        // FROM PictureSets ps
        //   LEFT JOIN (
        //   SELECT p.SetId, p.ThumbnailUrl,
        //   row_number() OVER (PARTITION BY p.SetId ORDER BY p.Rating DESC, p.PhotographedAt DESC) AS rown
        //   FROM Pictures p) x
        // ON x.SetId = ps.Id AND x.rown <= 4
        // GROUP BY ps.Id, ps.Name 
        return (await Task.WhenAll(pictureSets.Select(async ps =>
        {
            var thumbnailUrls = await dbContext.Pictures
                .Where(p => p.SetId == ps.Id)
                .OrderByDescending(p => p.Rating)
                .ThenByDescending(p => p.PhotographedAt)
                .Select(p => p.ThumbnailUrl)
                .Take(4)
                .ToListAsync();
            return new KeyValuePair<int, IEnumerable<string>>(ps.Id, thumbnailUrls);
        }))).ToDictionary();
    }
}