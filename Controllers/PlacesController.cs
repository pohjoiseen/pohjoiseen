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
using KoTi.ResponseModels;

namespace KoTi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PlacesController : ControllerBase
    {
        private readonly KoTiDbContext _context;

        public PlacesController(KoTiDbContext context)
        {
            _context = context;
        }
        
        // GET: api/Places/5
        [HttpGet("{id}")]
        public async Task<ActionResult<PlaceResponseDTO>> GetPlace(int id)
        {
            // TODO: this is repeated in AreasController.GetPlacesForArea()
            var result  = await _context.Places
                .GroupJoin(_context.Pictures, p => p.Id, pi => pi.PlaceId, (p, pictures) =>
                    new {
                        place = p,
                        thumbnailUrl = pictures
                            .OrderByDescending(pi => pi.Rating)
                            .ThenByDescending(pi => pi.PhotographedAt)
                            .Select(pi => pi.ThumbnailUrl)
                            .FirstOrDefault()
                    })
                .Where(p => p.place.Id == id)
                .FirstOrDefaultAsync();
            if (result == null)
            {
                return NotFound();
            }

            return PlaceResponseDTO.FromModel(result.place, result.thumbnailUrl);
        }

        // PUT: api/Places/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutPlace(int id, PlaceRequestDTO requestDto)
        {
            var place = await _context.Places.FindAsync(id);
            if (place == null)
            {
                return NotFound();
            }
            
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            requestDto.ToModel(place);
            
            _context.Entry(place).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PlaceExists(id))
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

        // POST: api/Places
        [HttpPost]
        public async Task<ActionResult<Place>> PostPlace(PlaceRequestDTO requestDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var place = new Place();
            requestDto.ToModel(place);
            if (place.Order == 0)
            {
                place.Order = await _context.Places.Where(p => p.AreaId == place.AreaId).CountAsync() + 1;
            }

            _context.Places.Add(place);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetPlace", new { id = place.Id }, place);
        }

        // DELETE: api/Places/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePlace(int id)
        {
            var place = await _context.Places.FindAsync(id);
            if (place == null)
            {
                return NotFound();
            }

            _context.Places.Remove(place);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool PlaceExists(int id)
        {
            return (_context.Places?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
