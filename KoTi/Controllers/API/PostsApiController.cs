using Holvi;
using Holvi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KoTi.Controllers.API;

[Route("api/Posts")]
[ApiController]
public class PostsApiController(HolviDbContext dbContext) : ControllerBase
{
    // GET: api/Posts/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Post>> GetPost(int id)
    {
        var post = await dbContext.Posts
            .Include(p => p.TitlePicture)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (post == null)
        {
            return NotFound();
        }

        return post;
    }
}