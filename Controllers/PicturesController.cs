using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KoTi.Models;
using KoTi.RequestModels;
using KoTi.ResponseModels;

namespace KoTi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PicturesController : ControllerBase
{
    private readonly KoTiDbContext _context;

    public PicturesController(KoTiDbContext context)
    {
        _context = context;
    }
    
    // GET: api/Pictures
    [HttpGet]
    public async Task<ActionResult<ListWithTotal<PictureResponseDTO>>> GetPictures(
        [FromQuery] int limit,
        [FromQuery] int offset)
    {
        var query = _context.Pictures
            .Include(p => p.Place)
            .AsQueryable();

        query = query.OrderBy(p => p.PhotographedAt);

        var queryPaginated = query;
        if (offset > 0)
        {
            queryPaginated = queryPaginated.Skip(offset);
        }
        if (limit > 0)
        {
            queryPaginated = queryPaginated.Take(limit);
        }

        return new ListWithTotal<PictureResponseDTO>
        {
            Total = await query.CountAsync(),
            Data = (await queryPaginated.ToListAsync()).Select(PictureResponseDTO.FromModel)
        };
    }

    // GET: api/Pictures/5
    [HttpGet("{id}")]
    public async Task<ActionResult<PictureResponseDTO>> GetPicture(int id)
    {
        var picture = await _context.Pictures
            .Include(p => p.Place)
            .Where(p => p.Id == id)
            .FirstOrDefaultAsync(); 
            
        if (picture == null)
        {
            return NotFound();
        }

        return PictureResponseDTO.FromModel(picture);
    }

    // PUT: api/Pictures/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutPicture(int id, PictureRequestDTO dto)
    {
        var picture = await _context.Pictures.FindAsync(id);
        if (picture == null)
        {
            return NotFound();
        }
        
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        dto.ToModel(picture);
        
        _context.Entry(picture).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!PictureExists(id))
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

    // POST: api/Pictures
    [HttpPost]
    public async Task<ActionResult<PictureResponseDTO>> PostPicture(PictureRequestDTO dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var picture = new Picture();
        dto.ToModel(picture);

        _context.Pictures.Add(picture);
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetPicture", new { id = picture.Id }, picture);
    }

    // DELETE: api/Pictures/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePicture(int id)
    {
        var picture = await _context.Pictures.FindAsync(id);
        if (picture == null)
        {
            return NotFound();
        }

        _context.Pictures.Remove(picture);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool PictureExists(int id)
    {
        return (_context.Pictures?.Any(e => e.Id == id)).GetValueOrDefault();
    }
}
