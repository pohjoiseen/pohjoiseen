using Holvi;
using Holvi.Models;
using KoTi.Suggestions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KoTi.Controllers.API;

/// <summary>
/// Suggested edits to content, see <see cref="Suggestion"/> and README.  Suggestions are created and withdrawn
/// from outside and declined from the edit page through this API; accepting happens only by saving the edit page.
/// Nothing here changes content.
/// </summary>
[Route("api/Suggestions")]
[ApiController]
public class SuggestionsApiController(HolviDbContext dbContext, SuggestionService suggestionService) : ControllerBase
{
    public record SuggestionResponse(
        int Id, string TargetType, int TargetId, string Kind, string OldText, string NewText, string Comment,
        IList<string> Sources, string Status, string Batch, DateTime CreatedAt, DateTime? ResolvedAt)
    {
        public static SuggestionResponse From(Suggestion s) => new(
            s.Id, s.TargetType, s.TargetId, s.Kind, s.OldText, s.NewText, s.Comment,
            s.Sources.Split('\n', StringSplitOptions.RemoveEmptyEntries), s.Status.ToString().ToLower(), s.Batch,
            s.CreatedAt, s.ResolvedAt);
    }

    public record CreateRequest(string? TargetType, int TargetId, string? Batch, IList<SuggestionService.NewSuggestion>? Suggestions);

    public record RejectedSuggestion(int Index, string Error);

    public record CreateResponse(IList<SuggestionResponse> Created, IList<RejectedSuggestion> Rejected);

    public record StatusRequest(string? Status);

    // GET: api/Suggestions?targetType=post&targetId=5&status=pending&batch=...
    [HttpGet]
    public async Task<ActionResult<IList<SuggestionResponse>>> GetSuggestions(
        [FromQuery] string? targetType, [FromQuery] int? targetId, [FromQuery] string? status, [FromQuery] string? batch)
    {
        var query = dbContext.Suggestions.AsQueryable();
        if (targetType is not null)
        {
            query = query.Where(s => s.TargetType == targetType);
        }
        if (targetId is not null)
        {
            query = query.Where(s => s.TargetId == targetId);
        }
        if (status is not null)
        {
            if (!Enum.TryParse<SuggestionStatus>(status, true, out var parsedStatus))
            {
                return BadRequest("status must be one of: pending, accepted, declined");
            }
            query = query.Where(s => s.Status == parsedStatus);
        }
        if (batch is not null)
        {
            query = query.Where(s => s.Batch == batch);
        }

        return (await query.OrderBy(s => s.Id).ToListAsync()).Select(SuggestionResponse.From).ToList();
    }

    // POST: api/Suggestions
    // Suggestions for one post/article/book.  Those which do not apply to its current text are rejected
    // individually, the rest are created.
    [HttpPost]
    public async Task<ActionResult<CreateResponse>> CreateSuggestions(CreateRequest request)
    {
        if (request.TargetType is null || !Suggestion.TargetTypes.Contains(request.TargetType))
        {
            return BadRequest($"targetType must be one of: {String.Join(", ", Suggestion.TargetTypes)}");
        }
        if (String.IsNullOrWhiteSpace(request.Batch) || request.Batch.Length > 100)
        {
            return BadRequest("batch is required (up to 100 characters)");
        }
        var content = await suggestionService.GetContentAsync(request.TargetType, request.TargetId);
        if (content is null)
        {
            return NotFound($"No {request.TargetType} with id {request.TargetId}");
        }

        var pending = await suggestionService.GetPendingAsync(request.TargetType, request.TargetId);
        var created = new List<Suggestion>();
        var rejected = new List<RejectedSuggestion>();
        var inputs = request.Suggestions ?? [];
        for (var i = 0; i < inputs.Count; i++)
        {
            var (suggestion, error) = suggestionService.Validate(inputs[i], content, pending);
            if (suggestion is null)
            {
                rejected.Add(new RejectedSuggestion(i, error!));
                continue;
            }

            suggestion.TargetType = request.TargetType;
            suggestion.TargetId = request.TargetId;
            suggestion.Batch = request.Batch.Trim();
            created.Add(suggestion);
            pending.Add(suggestion);
        }

        dbContext.Suggestions.AddRange(created);
        await dbContext.SaveChangesAsync();
        return new CreateResponse(created.Select(SuggestionResponse.From).ToList(), rejected);
    }

    // PUT: api/Suggestions/5/status
    // Decline a pending suggestion or make a declined one pending again.
    [HttpPut("{id}/status")]
    public async Task<ActionResult<SuggestionResponse>> SetStatus(int id, StatusRequest request)
    {
        var suggestion = await dbContext.Suggestions.FindAsync(id);
        if (suggestion is null)
        {
            return NotFound();
        }
        if (!Enum.TryParse<SuggestionStatus>(request.Status, true, out var status) || status == SuggestionStatus.Accepted)
        {
            return BadRequest("status must be declined or pending; suggestions are accepted by saving the edit page");
        }
        if (suggestion.Status == SuggestionStatus.Accepted)
        {
            return Conflict("Suggestion is already accepted");
        }

        suggestion.Status = status;
        suggestion.ResolvedAt = status == SuggestionStatus.Declined ? DateTime.UtcNow : null;
        await dbContext.SaveChangesAsync();
        return SuggestionResponse.From(suggestion);
    }

    // DELETE: api/Suggestions?batch=...
    // Withdraw a batch: deletes its suggestions which are still pending, returns how many.
    [HttpDelete]
    public async Task<ActionResult<int>> DeleteBatch([FromQuery] string? batch)
    {
        if (String.IsNullOrWhiteSpace(batch))
        {
            return BadRequest("batch is required");
        }

        return await dbContext.Suggestions
            .Where(s => s.Batch == batch && s.Status == SuggestionStatus.Pending)
            .ExecuteDeleteAsync();
    }
}
