using Holvi;
using Holvi.Models;
using KoTi.Suggestions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KoTi.Controllers.API;

[Route("api/Articles")]
[ApiController]
public class ArticlesApiController(HolviDbContext dbContext, SuggestionService suggestionService) : ControllerBase
{
    public record ArticleSummary(int Id, string Language, string Name, string Title, bool Draft,
        DateTime UpdatedAt, int ContentLength, int PendingSuggestions);

    // GET: api/Articles?language=ru
    // All articles without content.
    [HttpGet]
    public async Task<ActionResult<IList<ArticleSummary>>> GetArticles([FromQuery] string? language)
    {
        var pending = await suggestionService.GetPendingCountsAsync(Suggestion.Article);
        var articles = await dbContext.Articles
            .Where(a => language == null || a.Language == language)
            .OrderBy(a => a.Name)
            .Select(a => new { a.Id, a.Language, a.Name, a.Title, a.Draft, a.UpdatedAt, a.ContentMD.Length })
            .ToListAsync();
        return articles
            .Select(a => new ArticleSummary(a.Id, a.Language, a.Name, a.Title, a.Draft, a.UpdatedAt, a.Length,
                pending.GetValueOrDefault(a.Id)))
            .ToList();
    }

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