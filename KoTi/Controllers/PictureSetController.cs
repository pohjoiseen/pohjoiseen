using Holvi;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KoTi.RequestModels;
using Holvi.Models;
using KoTi.ResponseModels;

namespace KoTi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PictureSetsController : ControllerBase
{
    private readonly HolviDbContext _context;

    public PictureSetsController(HolviDbContext context)
    {
        _context = context;
    }

    // GET: api/PictureSets
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PictureSetResponseDTO>>> GetPictureSets()
    {
        var pictureSets = await _context.PictureSets
            .Where(ps => ps.ParentId == null)
            .OrderBy(p => p.Name)
            .ToListAsync();
        return Ok(await CreateResponseWithThumbnails(pictureSets));
    }
    
    // GET: api/PictureSets/5
    [HttpGet("{id}")]
    public async Task<ActionResult<PictureSetResponseDTO>> GetPictureSet(int id)
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

        return PictureSetResponseDTO.FromModel(pictureSet, new List<string>(),
            await CreateResponseWithThumbnails(pictureSet.Children));
    }

    // PUT: api/PictureSets/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutPictureSet(int id, PictureSetRequestDTO dto)
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
    public async Task<ActionResult<PictureSet>> PostPictureSet(PictureSetRequestDTO dto)
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

    private async Task<IEnumerable<PictureSetResponseDTO>> CreateResponseWithThumbnails(IEnumerable<PictureSet> pictureSets)
    {
        // N+1 queries, but should be alright with SQLite?
        // otherwise would need to drop into fully raw SQL
        // with something like:
        // SELECT ps.*, ps.Name, GROUP_CONCAT(x.ThumbnailUrl)
        // FROM PictureSets ps
        //   LEFT JOIN (
        //   SELECT p.SetId, p.ThumbnailUrl,
        //   row_number() OVER (PARTITION BY p.SetId ORDER BY p.Rating DESC, p.PhotographedAt DESC) AS rown
        //   FROM Pictures p) x
        // ON x.SetId = ps.Id AND x.rown <= 4
        // GROUP BY ps.Id, ps.Name 
        return await Task.WhenAll(pictureSets.Select(async ps =>
        {
            var thumbnailUrls = await _context.Pictures
                .Where(p => p.SetId == ps.Id)
                .OrderByDescending(p => p.Rating)
                .ThenByDescending(p => p.PhotographedAt)
                .Select(p => p.ThumbnailUrl)
                .Take(4)
                .ToListAsync();
            return PictureSetResponseDTO.FromModel(ps, thumbnailUrls);
        }));
    }
}