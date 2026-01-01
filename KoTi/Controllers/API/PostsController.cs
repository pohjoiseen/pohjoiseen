using Holvi;
using Holvi.Models;
using KoTi.RequestModels;
using KoTi.ResponseModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KoTi.Controllers.API;

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
    
    // PUT: api/Posts/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutPost(int id, PostRequestDTO requestDto)
    {
        var post = await dbContext.Posts
            .Where(p => p.Id == id)
            .FirstOrDefaultAsync();
        if (post == null)
        {
            return NotFound();
        }
        
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        requestDto.ToModel(post);
        
        dbContext.Entry(post).State = EntityState.Modified;

        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!PostExists(id))
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

    // POST: api/Posts
    [HttpPost]
    public async Task<ActionResult<Post>> PostPost(PostRequestDTO requestDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var post = new Post { Language = Fennica3.Fennica3.Languages[0] };
        requestDto.ToModel(post);
        dbContext.Posts.Add(post);
        await dbContext.SaveChangesAsync();

        return CreatedAtAction("GetPost", new { id = post.Id }, post);
    }

    // DELETE: api/Posts/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePost(int id)
    {
        var post = await dbContext.Posts.FindAsync(id);
        if (post == null)
        {
            return NotFound();
        }

        dbContext.Posts.Remove(post);
        await dbContext.SaveChangesAsync();

        return NoContent();
    }

    private bool PostExists(int id)
    {
        return dbContext.Posts.Any(e => e.Id == id);
    }
}