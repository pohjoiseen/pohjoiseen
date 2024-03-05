using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KoTi.RequestModels;
using KoTi.Models;

namespace KoTi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PictureSetsController : ControllerBase
{
    private readonly KoTiDbContext _context;

    public PictureSetsController(KoTiDbContext context)
    {
        _context = context;
    }

    // GET: api/PictureSets
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PictureSet>>> GetPictureSets()
    {
        return await _context.PictureSets
            .Where(ps => ps.ParentId == null)
            .OrderBy(p => p.Name)
            .ToListAsync();
    }

    
    // GET: api/PictureSets/5
    [HttpGet("{id}")]
    public async Task<ActionResult<PictureSet>> GetPictureSet(int id)
    {
        var pictureSet = await _context.PictureSets
            .Include(ps => ps.Children)
            .Where(ps => ps.Id == id)
            .OrderBy(ps => ps.Name)
            .FirstOrDefaultAsync();
        if (pictureSet == null)
        {
            return NotFound();
        }

        return pictureSet;
    }

    // PUT: api/PictureSets/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutPictureSet(int id, PictureSetDTO dto)
    {
        var pictureSet = await _context.PictureSets.FindAsync(id);
        if (pictureSet == null)
        {
            return NotFound();
        }
            
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        dto.ToModel(pictureSet);
            
        _context.Entry(pictureSet).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!PictureSetExists(id))
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

    // POST: api/PictureSets
    [HttpPost]
    public async Task<ActionResult<PictureSet>> PostPictureSet(PictureSetDTO dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var pictureSet = new PictureSet();
        dto.ToModel(pictureSet);

        _context.PictureSets.Add(pictureSet);
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetPictureSet", new { id = pictureSet.Id }, pictureSet);
    }

    // DELETE: api/PictureSets/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePictureSet(int id)
    {
        var pictureSet = await _context.PictureSets.FindAsync(id);
        if (pictureSet == null)
        {
            return NotFound();
        }

        _context.PictureSets.Remove(pictureSet);
        await _context.Pictures
            .Where(p => p.SetId == id)
            .ExecuteUpdateAsync(spc => spc.SetProperty(p => p.SetId, p => pictureSet.ParentId));
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // POST: api/PictureSets/5/MovePictures
    [HttpPost("{id}/MovePictures")]
    public async Task<IActionResult> MovePicturesIntoSet(int id, IdsDTO dto)
    {
        var pictureSet = await _context.PictureSets.FindAsync(id);
        if (pictureSet == null)
        {
            return NotFound();
        }

        var pictures = await _context.Pictures
            .Where(p => dto.Ids.Contains(p.Id))
            .ToListAsync();
        foreach (var p in pictures)
        {
            p.SetId = id;
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }

    // POST: api/PictureSets/RemovePicturesFromSets
    [HttpPost("RemovePicturesFromSets")]
    public async Task<IActionResult> RemovePicturesFromSets(IdsDTO dto)
    {
        var pictures = await _context.Pictures
            .Where(p => dto.Ids.Contains(p.Id))
            .ToListAsync();
        foreach (var p in pictures)
        {
            p.SetId = null;
        }
        
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private bool PictureSetExists(int id)
    {
        return (_context.PictureSets?.Any(e => e.Id == id)).GetValueOrDefault();
    }
}