using Holvi;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KoTi.ResponseModels;
using Microsoft.Data.Sqlite;

namespace KoTi.Controllers.API;

[Route("api/Search")]
[ApiController]
public class SearchApiController : ControllerBase
{
    private readonly HolviDbContext _context;

    public SearchApiController(HolviDbContext context)
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
                return $"/app/Places/{tableId}";
            
            case "PictureSets":
                return $"/pictures/folders/?folderId={tableId}";
            
            case "Posts":
                return $"/app/Posts/{tableId}/";
            
            case "Articles":
                return $"/app/Articles/{tableId}/";

            default:
                throw new Exception("Unknown table name: " + tableName);
        }
    }
}