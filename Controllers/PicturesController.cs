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
public class PicturesController : ControllerBase
{
    private readonly KoTiDbContext _context;

    public PicturesController(KoTiDbContext context)
    {
        _context = context;
    }
    
    // GET: api/Pictures
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Picture>>> GetPictures()
    {
        return await _context.Pictures.OrderBy(p => p.PhotographedAt).ToListAsync();
    }

    // GET: api/Pictures/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Picture>> GetPicture(int id)
    {
        var picture = await _context.Pictures.FindAsync(id);
        if (picture == null)
        {
            return NotFound();
        }

        return picture;
    }

    // PUT: api/Pictures/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutPicture(int id, PictureDTO dto)
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
    public async Task<ActionResult<Picture>> PostPicture(PictureDTO dto)
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
