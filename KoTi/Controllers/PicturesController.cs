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
    private readonly PictureStorage _pictureStorage;

    public PicturesController(KoTiDbContext context, PictureStorage pictureStorage)
    {
        _context = context;
        _pictureStorage = pictureStorage;
    }
    
    // GET: api/Pictures
    [HttpGet]
    public async Task<ActionResult<ListWithTotal<PictureResponseDTO>>> GetPictures(
        [FromQuery] int limit,
        [FromQuery] int offset,
        [FromQuery] int? setId,
        [FromQuery] int? countryId,
        [FromQuery] int? regionId,
        [FromQuery] int? placeId,
        [FromQuery] int? areaId,
        [FromQuery(Name = "tagIds")] IList<int>? tagIds,
        [FromQuery] int? minRating)
    {
        var query = _context.Pictures
            .Include(p => p.Place)
            .ThenInclude(pl => pl!.Area)
            .ThenInclude(a => a.Region)
            .Include(p => p.Set)
            .Include(p => p.Tags)
            .AsQueryable();

        query = query.OrderBy(p => p.PhotographedAt);
        if (setId != null)
        {
            if (setId > 0)
            {
                query = query.Where(p => p.SetId == setId);
            }
            else
            {
                query = query.Where(p => p.SetId == null);
            }
        }

        if (placeId != null)
        {
            query = query.Where(p => p.PlaceId == placeId);
        }

        if (areaId != null)
        {
            query = query.Where(p => p.Place != null && p.Place.AreaId == areaId);
        }

        if (regionId != null)
        {
            query = query.Where(p => p.Place != null && p.Place.Area.RegionId == regionId);
        }

        if (countryId != null)
        {
            query = query.Where(p => p.Place != null && p.Place.Area.Region.CountryId == countryId);
        }

        if (minRating != null)
        {
            query = query.Where(p => p.Rating >= minRating);
        }

        if (tagIds != null)
        {
            foreach (var tagId in tagIds)
            {
                query = query.Where(p => p.Tags.Any(t => t.Id == tagId));
            }
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

        return new ListWithTotal<PictureResponseDTO>
        {
            Total = await query.CountAsync(),
            Data = (await queryPaginated.ToListAsync()).Select(PictureResponseDTO.FromModel)
        };
    }
    
    // GET: api/Pictures/ForPlace/5
    [HttpGet("ForPlace/{placeId}")]
    public async Task<ActionResult<IEnumerable<PictureResponseDTO>>> GetPicturesForPlace(int placeId, [FromQuery] int limit)
    {
        var pictures = await _context.Pictures
            .Include(p => p.Place)
            .Include(p => p.Set)
            .Include(p => p.Tags)
            .Where(p => p.PlaceId == placeId)
            .OrderByDescending(p => p.Rating)
            .ThenByDescending(p => p.PhotographedAt)
            .Take(limit > 0 ? limit : 25)
            .ToListAsync();
        return Ok(pictures.Select(PictureResponseDTO.FromModel));
    }
    
    // GET: api/Pictures/ForeArea/5
    [HttpGet("ForArea/{areaId}")]
    public async Task<ActionResult<IEnumerable<PictureResponseDTO>>> GetPicturesForArea(int areaId, [FromQuery] int limit)
    {
        var pictures = await _context.Pictures
            .Include(p => p.Place)
            .Include(p => p.Set)
            .Include(p => p.Tags)
            .Where(p => p.Place != null && p.Place.AreaId == areaId)
            .OrderByDescending(p => p.Rating)
            .ThenByDescending(p => p.PhotographedAt)
            .Take(limit > 0 ? limit : 25)
            .ToListAsync();
        return Ok(pictures.Select(PictureResponseDTO.FromModel));
    }

    // GET: api/Pictures/5
    [HttpGet("{id}")]
    public async Task<ActionResult<PictureResponseDTO>> GetPicture(int id)
    {
        var picture = await _context.Pictures
            .Include(p => p.Place)
            .Include(p => p.Set)
            .Include(p => p.Tags)
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
        var picture = await _context.Pictures
            .Include(p => p.Tags)
            .Where(p => p.Id == id)
            .FirstOrDefaultAsync();
        if (picture == null)
        {
            return NotFound();
        }
        
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        dto.ToModel(picture, _context);
        
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
        dto.ToModel(picture, _context);

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

        if (!picture.ThumbnailUrl.Equals(picture.Url))
        {
            await _pictureStorage.DeletePictureAsync(picture.ThumbnailUrl);
        }

        if (!picture.DetailsUrl.Equals(picture.Url))
        {
            await _pictureStorage.DeletePictureAsync(picture.DetailsUrl);
        }

        await _pictureStorage.DeletePictureAsync(picture.Url.Replace(_pictureStorage.PublicUrl, ""));

        return NoContent();
    }
    
    // DELETE: api/Pictures
    [HttpDelete]
    public async Task<IActionResult> DeletePictureMultiple(IdsDTO ids)
    {
        foreach (var id in ids.Ids)
        {
            var picture = await _context.Pictures.FindAsync(id);
            if (picture == null)
            {
                continue;
            }

            _context.Pictures.Remove(picture);
            // sync after every picture, yes
            await _context.SaveChangesAsync();

            if (!picture.ThumbnailUrl.Equals(picture.Url))
            {
                await _pictureStorage.DeletePictureAsync(picture.ThumbnailUrl.Replace(_pictureStorage.PublicUrl, ""));
            }

            if (!picture.DetailsUrl.Equals(picture.Url))
            {
                await _pictureStorage.DeletePictureAsync(picture.DetailsUrl.Replace(_pictureStorage.PublicUrl, ""));
            }

            await _pictureStorage.DeletePictureAsync(picture.Url.Replace(_pictureStorage.PublicUrl, ""));
        }

        return NoContent();
    }


    private bool PictureExists(int id)
    {
        return (_context.Pictures?.Any(e => e.Id == id)).GetValueOrDefault();
    }
}
