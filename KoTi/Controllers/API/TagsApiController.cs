using Holvi;
using Holvi.Models;
using KoTi.RequestModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KoTi.Controllers.API;

[Route("api/Tags")]
[ApiController]
public class TagsApiController : ControllerBase
{
    private readonly HolviDbContext _context;

    public TagsApiController(HolviDbContext context)
    {
        _context = context;
    }

    // GET: api/Tags
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Tag>>> Search([FromQuery] string q)
    {
        // note that LIKE in Sqlite (which .StartsWith() will translate to) is case-insensitive
        // for ASCII by default, which is what we want
        return await _context.Tags
            .Where(t => t.Name.StartsWith(q))
            .OrderBy(t => t.Name)
            .Take(50)
            .ToListAsync();
    }
    
    // GET: api/Tags/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Tag>> GetTag(int id)
    {
        var tag = await _context.Tags.FindAsync(id);
        if (tag == null)
        {
            return NotFound();
        }

        return tag;
    }
    
    // POST: api/Tags
    [HttpPost]
    public async Task<ActionResult<Tag>> PostPictureSet(TagDTO dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var tag = new Tag();
        dto.ToModel(tag);

        _context.Tags.Add(tag);
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetTag", new { id = tag.Id }, tag);
    }
}