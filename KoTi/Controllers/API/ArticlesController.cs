using Holvi;
using Holvi.Models;
using KoTi.RequestModels;
using KoTi.ResponseModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KoTi.Controllers.API;

[Route("api/[controller]")]
[ApiController]
public class ArticlesController(HolviDbContext dbContext) : ControllerBase
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
    
    // PUT: api/Articles/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutArticle(int id, ArticleRequestDTO requestDto)
    {
        var article = await dbContext.Articles
            .Where(a => a.Id == id)
            .FirstOrDefaultAsync();
        if (article == null)
        {
            return NotFound();
        }
        
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        requestDto.ToModel(article);
        
        dbContext.Entry(article).State = EntityState.Modified;

        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ArticleExists(id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return NoContent();
    }

    // POST: api/Articles
    [HttpPost]
    public async Task<ActionResult<Article>> PostArticle(ArticleRequestDTO requestDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var article = new Article { Language = Fennica3.Fennica3.Languages[0] };
        requestDto.ToModel(article);
        dbContext.Articles.Add(article);
        await dbContext.SaveChangesAsync();

        return CreatedAtAction("GetArticle", new { id = article.Id }, article);
    }

    // DELETE: api/Articles/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteArticle(int id)
    {
        var article = await dbContext.Articles.FindAsync(id);
        if (article == null)
        {
            return NotFound();
        }

        dbContext.Articles.Remove(article);
        await dbContext.SaveChangesAsync();

        return NoContent();
    }

    private bool ArticleExists(int id)
    {
        return dbContext.Articles.Any(e => e.Id == id);
    }
}