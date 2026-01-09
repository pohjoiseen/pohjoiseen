using Holvi;
using KoTi.RequestModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Holvi.Models;

namespace KoTi.Controllers.API;

[Route("api/Regions")]
[ApiController]
public class RegionsApiController : ControllerBase
{
    private readonly HolviDbContext _context;

    public RegionsApiController(HolviDbContext context)
    {
        _context = context;
    }
    
    // GET: api/Regions/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Region>> GetRegion(int id)
    {
        var region = await _context.Regions.FindAsync(id);
        if (region == null)
        {
            return NotFound();
        }

        return region;
    }

    // PUT: api/Regions/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutRegion(int id, RegionDTO dto)
    {
        var region = await _context.Regions.FindAsync(id);
        if (region == null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        dto.ToModel(region);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!RegionExists(id))
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

    // POST: api/Regions
    [HttpPost]
    public async Task<ActionResult<Region>> PostRegion(RegionDTO dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var region = new Region();
        dto.ToModel(region);
        
        _context.Regions.Add(region);
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetRegion", new { id = region.Id }, region);
    }

    // DELETE: api/Regions/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRegion(int id)
    {
        var region = await _context.Regions.FindAsync(id);
        if (region == null)
        {
            return NotFound();
        }

        _context.Regions.Remove(region);
        await _context.SaveChangesAsync();

        return NoContent();
    }
    
    // GET: api/Regions/{id}/Areas
    [HttpGet("{id}/Areas")]
    public async Task<ActionResult<IEnumerable<Area>>> GetAreasForRegion(int id)
    {
        return await _context.Areas
            .Where(a => a.RegionId == id)
            .OrderBy(a => a.Order)
            .ToListAsync();
    }

    // PUT: api/Regions/{id}/Areas/Order
    [HttpPut("{id}/Areas/Order")]
    public async Task<IActionResult> ReorderAreas(int id, IdsDTO dto)
    {
        var areasById = await _context.Areas
            .Where(a => a.RegionId == id)
            .ToDictionaryAsync(a => a.Id, a => a);
        for (int i = 0; i < dto.Ids.Length; i++)
        {
            if (!areasById.ContainsKey(dto.Ids[i]))
            {
                return BadRequest();
            }

            areasById[dto.Ids[i]].Order = i + 1;
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }
    
    private bool RegionExists(int id)
    {
        return (_context.Regions?.Any(e => e.Id == id)).GetValueOrDefault();
    }
}