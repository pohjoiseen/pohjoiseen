using Holvi;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Holvi.Models;
using KoTi.ResponseModels;

namespace KoTi.Controllers.API;

[Route("api/Pictures")]
[ApiController]
public class PicturesApiController(HolviDbContext dbContext, PictureUpload pictureUpload) : ControllerBase
{
    // GET: api/Pictures/5
    [HttpGet("{id}")]
    public async Task<ActionResult<PictureResponseDTO>> GetPicture(int id)
    {
        var picture = await dbContext.Pictures
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

    // POST: api/Pictures/5/WebSizes
    [HttpPost("{id}/WebSizes")]
    public async Task<IActionResult> EnsureWebSizes(int id)
    {
        var picture = await dbContext.Pictures.FindAsync(id);
        if (picture == null)
        {
            return NotFound();
        }

        await pictureUpload.EnsureWebsiteVersionsExist(picture);
        return NoContent();
    }
}
