using Holvi;
using Holvi.Models;
using KoTi.ResponseModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KoTi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class RedirectsController(HolviDbContext dbContext) : ControllerBase
{
    // GET: api/Redirects
    [HttpGet]
    public async Task<ActionResult<ListWithTotal<Redirect>>> GetPictures(
        [FromQuery] int limit,
        [FromQuery] int offset)
    {
        var query = dbContext.Redirects
            .OrderBy(r => r.UrlFrom)
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

        return new ListWithTotal<Redirect>
        {
            Total = await query.CountAsync(),
            Data = await queryPaginated.ToListAsync()
        };
    }
    
    // POST: api/Redirects
    [HttpPost]
    public async Task<IActionResult> PostRedirect(Redirect redirect)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        
        var existing = await dbContext.Redirects.FirstOrDefaultAsync(r => r.UrlFrom == redirect.UrlFrom);
        if (existing is not null)
        {
            return BadRequest(new { title = "Redirect already exists" });
        }
        
        dbContext.Redirects.Add(redirect);
        await dbContext.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/Redirects/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRedirect(int id)
    {
        var redirect = await dbContext.Redirects.FindAsync(id);
        if (redirect == null)
        {
            return NotFound();
        }

        dbContext.Redirects.Remove(redirect);
        await dbContext.SaveChangesAsync();

        return NoContent();
    }
}