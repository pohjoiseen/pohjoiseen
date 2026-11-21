using Holvi;
using Holvi.Models;
using KoTi.Suggestions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KoTi.Controllers.API;

[Route("api/Books")]
[ApiController]
public class BooksApiController(HolviDbContext dbContext, SuggestionService suggestionService) : ControllerBase
{
    public record BookSummary(int Id, string Language, string Name, string Title, bool Draft,
        DateTime UpdatedAt, int ContentLength, int PendingSuggestions);

    public record BookResponse(int Id, string Language, string Name, string Title, bool Draft,
        DateTime UpdatedAt, string ContentMD, IList<int> PostIds);

    // GET: api/Books?language=ru
    // All books without content.
    [HttpGet]
    public async Task<ActionResult<IList<BookSummary>>> GetBooks([FromQuery] string? language)
    {
        var pending = await suggestionService.GetPendingCountsAsync(Suggestion.Book);
        var books = await dbContext.Books
            .Where(b => language == null || b.Language == language)
            .OrderBy(b => b.Name)
            .Select(b => new { b.Id, b.Language, b.Name, b.Title, b.Draft, b.UpdatedAt, b.ContentMD.Length })
            .ToListAsync();
        return books
            .Select(b => new BookSummary(b.Id, b.Language, b.Name, b.Title, b.Draft, b.UpdatedAt, b.Length,
                pending.GetValueOrDefault(b.Id)))
            .ToList();
    }

    // GET: api/Books/5
    [HttpGet("{id}")]
    public async Task<ActionResult<BookResponse>> GetBook(int id)
    {
        var book = await dbContext.Books.FirstOrDefaultAsync(b => b.Id == id);
        if (book == null)
        {
            return NotFound();
        }

        var postIds = await dbContext.Posts.Where(p => p.BookId == id).OrderBy(p => p.Order).Select(p => p.Id).ToListAsync();
        return new BookResponse(book.Id, book.Language, book.Name, book.Title, book.Draft, book.UpdatedAt, book.ContentMD, postIds);
    }
}
