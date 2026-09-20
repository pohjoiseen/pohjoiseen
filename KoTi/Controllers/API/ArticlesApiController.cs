using Holvi;
using Holvi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KoTi.Controllers.API;

[Route("api/Articles")]
[ApiController]
public class ArticlesApiController(HolviDbContext dbContext) : ControllerBase
{
    // GET: api/Articles/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Article>> GetArticle(int id)
    {
        var article = await dbContext.Articles
            .FirstOrDefaultAsync(a => a.Id == id);
        if (article == null)
        {
            return NotFound();
        }

        return article;
    }
}