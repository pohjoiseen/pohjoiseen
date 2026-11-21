using Holvi;
using Holvi.Models;
using KoTi.Suggestions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KoTi.Controllers.API;

[Route("api/Posts")]
[ApiController]
public class PostsApiController(HolviDbContext dbContext, SuggestionService suggestionService) : ControllerBase
{
    public record PostSummary(int Id, string Language, string Name, DateOnly Date, string Title, bool Draft,
        DateTime UpdatedAt, int ContentLength, int PendingSuggestions);

    // GET: api/Posts?language=ru
    // All posts without content, oldest first.
    [HttpGet]
    public async Task<ActionResult<IList<PostSummary>>> GetPosts([FromQuery] string? language)
    {
        var pending = await suggestionService.GetPendingCountsAsync(Suggestion.Post);
        var posts = await dbContext.Posts
            .Where(p => language == null || p.Language == language)
            .OrderBy(p => p.Date).ThenBy(p => p.Name)
            .Select(p => new { p.Id, p.Language, p.Name, p.Date, p.Title, p.Draft, p.UpdatedAt, p.ContentMD.Length })
            .ToListAsync();
        return posts
            .Select(p => new PostSummary(p.Id, p.Language, p.Name, p.Date, p.Title, p.Draft, p.UpdatedAt, p.Length,
                pending.GetValueOrDefault(p.Id)))
            .ToList();
    }

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