using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KoTi;
using KoTi.RequestModels;
using KoTi.Models;
using KoTi.ResponseModels;
using Microsoft.Data.Sqlite;

namespace KoTi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SearchController : ControllerBase
{
    private readonly KoTiDbContext _context;

    public SearchController(KoTiDbContext context)
    {
        _context = context;
    }
    
    // GET: api/Search
    [HttpGet]
    public async Task<ActionResult<ListWithTotal<SearchResult>>> Search([FromQuery] string q, [FromQuery] string? tables,
        [FromQuery] int limit, [FromQuery] int offset)
    {
        var query = _context.Database
            .SqlQuery<SearchResult>(
                $"SELECT TableName, TableId, Title, snippet(Search, 3, '<b>', '</b>', '...', 25) AS Text, bm25(Search, 1.0, 1.0, 3.0) AS Rank FROM Search({q})");

        if (tables != null)
        {
            var tablesArray = tables.Split(',');
            query = query.Where(sr => tablesArray.Contains(sr.TableName));
        }
        
        query = query.OrderBy(sr => sr.Rank);

        try
        {
            return new ListWithTotal<SearchResult>
            {
                Total = await query.CountAsync(),
                Data = await query.Skip(offset)
                    .Take(limit > 0 ? limit : 25)
                    .ToListAsync()
            };
        }
        catch (SqliteException e)
        {
            return StatusCode(500, new Error { Title = e.Message });
        }
    }

    [HttpGet("UrlFor")]
    public async Task<ActionResult<string>> UrlFor([FromQuery] string tableName, [FromQuery] int tableId)
    {
        switch (tableName)
        {
            case "Pictures":
                // TODO: open in fullscreen properly
                return "/pictures/all";

            case "Places":
            {
                var place = await _context.Places
                    .Include(p => p.Area)
                    .Include(p => p.Area.Region)
                    .Where(p => p.Id == tableId)
                    .FirstOrDefaultAsync();
                if (place == null)
                {
                    return NotFound();
                }
                
                return $"/country/{place.Area.Region.CountryId}/region/{place.Area.RegionId}/area/{place.AreaId}?place={place.Id}&filter={Uri.EscapeDataString(place.Name)}";
            }
            
            case "Areas":
            {
                var area = await _context.Areas
                    .Include(a => a.Region)
                    .Where(a => a.Id == tableId)
                    .FirstOrDefaultAsync();
                if (area == null)
                {
                    return NotFound();
                }

                return $"/country/{area.Region.CountryId}/region/{area.RegionId}/area/{area.Id}";
            }
            
            case "Regions":
            {
                var region = await _context.Regions.FindAsync(tableId);
                if (region == null)
                {
                    return NotFound();
                }

                return $"/country/{region.CountryId}/region/{region.Id}";
            }

            case "Countries":
                return $"/country/{tableId}";

            case "PictureSets":
                return $"/pictures/folders/?folderId={tableId}";

            default:
                throw new Exception("Unknown table name: " + tableName);
        }
    }
}