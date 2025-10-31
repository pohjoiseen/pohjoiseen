using Holvi;
using Holvi.Models;
using KoTi.ResponseModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KoTi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PostsController(HolviDbContext dbContext) : ControllerBase
{
    // GET: api/Posts
    [HttpGet]
    public async Task<ActionResult<ListWithTotal<Post>>> GetPictures(
        [FromQuery] int limit,
        [FromQuery] int offset,
        [FromQuery] string? search)
    {
        var query = dbContext.Posts
            .Include(p => p.TitlePicture)
            .OrderByDescending(p => p.Date)
            .ThenByDescending(p => p.Name)
            .AsQueryable();

        if (search != null)
        {
            query = query.Where(p => p.Name.Contains(search) || p.Title.Contains(search));
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

        return new ListWithTotal<Post>
        {
            Total = await query.CountAsync(),
            Data = await queryPaginated.ToListAsync()
        };
    }
    
    // GET: api/Posts/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Post>> GetPost(int id)
    {
        var post = await dbContext.Posts
            .Include(p => p.TitlePicture)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (post == null)
        {
            return NotFound();
        }

        return post;
    }

    
}