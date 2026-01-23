using Holvi;
using Holvi.Models;
using KoTi.RequestModels;
using KoTi.ResponseModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KoTi.Controllers.API;

[Route("api/Articles")]
[ApiController]
public class ArticlesApiController(HolviDbContext dbContext) : ControllerBase
{
    // GET: api/Articles
    [HttpGet]
    public async Task<ActionResult<ListWithTotal<Article>>> GetPictures(
        [FromQuery] int limit,
        [FromQuery] int offset)
    {
        var query = dbContext.Articles
            .OrderBy(a => a.Name)
            .AsQueryable();

        var queryPaginated = query;
        if (offset > 0)
        {
            queryPaginated = queryPaginated.Skip(offset);
        }
        if (limit > 0)
        {
            queryPaginated = queryPaginated.Take(limit);
        }

        return new ListWithTotal<Article>
        {
            Total = await query.CountAsync(),
            Data = await queryPaginated.ToListAsync()
        };
    }
    
    // GET: api/Articles/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Article>> GetArticle(int id)
    {
        var article = await dbContext.Articles
            .FirstOrDefaultAsync(a => a.Id == id);
        if (article == null)
        {
            return NotFound();
        }

        return article;
    }
}