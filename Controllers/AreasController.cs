using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KoTi;
using KoTi.RequestModels;
using KoTi.Models;

namespace KoTi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AreasController : ControllerBase
{
    private readonly KoTiDbContext _context;

    public AreasController(KoTiDbContext context)
    {
        _context = context;
    }
    
    // GET: api/Areas/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Area>> GetArea(int id)
    {
        var area = await _context.Areas.FindAsync(id);
        if (area == null)
        {
            return NotFound();
        }

        return area;
    }

    // PUT: api/Areas/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutArea(int id, AreaDTO dto)
    {
        var area = await _context.Areas.FindAsync(id);
        if (area == null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        dto.ToModel(area);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!AreaExists(id))
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

    // POST: api/Areas
    [HttpPost]
    public async Task<ActionResult<Area>> PostArea(AreaDTO dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var area = new Area();
        dto.ToModel(area);
    
        _context.Areas.Add(area);
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetArea", new { id = area.Id }, area);
    }

    // DELETE: api/Areas/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteArea(int id)
    {
        var area = await _context.Areas.FindAsync(id);
        if (area == null)
        {
            return NotFound();
        }

        _context.Areas.Remove(area);
        await _context.SaveChangesAsync();

        return NoContent();
    }
    
    // GET: api/Areas/{id}/Places
    [HttpGet("{id}/Places")]
    public async Task<ActionResult<IEnumerable<Place>>> GetPlacesForArea(int id)
    {
        return await _context.Places
            .Where(p => p.AreaId == id)
            .OrderBy(p => p.Order)
            .ToListAsync();
    }

    // PUT: api/Areas/{id}/Places/Order
    [HttpPut("{id}/Places/Order")]
    public async Task<IActionResult> ReorderPlaces(int id, OrderDTO dto)
    {
        var placesById = await _context.Places
            .Where(p => p.AreaId == id)
            .ToDictionaryAsync(p => p.Id, p => p);
        for (int i = 0; i < dto.Ids.Length; i++)
        {
            if (!placesById.ContainsKey(dto.Ids[i]))
            {
                return BadRequest();
            }

            placesById[dto.Ids[i]].Order = i + 1;
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }
    
    private bool AreaExists(int id)
    {
        return (_context.Areas?.Any(e => e.Id == id)).GetValueOrDefault();
    }
}
