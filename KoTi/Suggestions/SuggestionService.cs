using Holvi;
using Holvi.Models;
using Microsoft.EntityFrameworkCore;

namespace KoTi.Suggestions;

/// <summary>
/// Suggested edits to content (see <see cref="Suggestion"/>): what is shared between the API, which creates them,
/// and the edit pages, where they are accepted or declined.
/// </summary>
public class SuggestionService(HolviDbContext dbContext)
{
    public record NewSuggestion(string? Kind, string? OldText, string? NewText, string? Comment, IList<string>? Sources);

    /// <summary>
    /// Content of a post, article or book with normalized line endings, null if there is none with that id.
    /// </summary>
    public async Task<string?> GetContentAsync(string targetType, int targetId)
    {
        var content = targetType switch
        {
            Suggestion.Post => await dbContext.Posts.Where(p => p.Id == targetId).Select(p => p.ContentMD).FirstOrDefaultAsync(),
            Suggestion.Article => await dbContext.Articles.Where(a => a.Id == targetId).Select(a => a.ContentMD).FirstOrDefaultAsync(),
            Suggestion.Book => await dbContext.Books.Where(b => b.Id == targetId).Select(b => b.ContentMD).FirstOrDefaultAsync(),
            _ => null
        };
        return content is null ? null : Suggestion.NormalizeText(content);
    }

    /// <summary>
    /// Checks a new suggestion against the current content and pending suggestions of the target.
    /// Returns the suggestion to add, or the reason why not.
    /// </summary>
    public (Suggestion?, string? error) Validate(NewSuggestion input, string content, IList<Suggestion> pending)
    {
        var oldText = Suggestion.NormalizeText(input.OldText);
        var newText = Suggestion.NormalizeText(input.NewText);
        var kind = input.Kind ?? "";
        if (!Suggestion.Kinds.Contains(kind))
        {
            return (null, $"kind must be one of: {String.Join(", ", Suggestion.Kinds)}");
        }
        if (String.IsNullOrWhiteSpace(oldText))
        {
            return (null, "oldText is empty; to insert text, use a nearby sentence as oldText and repeat it in newText");
        }
        if (oldText == newText)
        {
            return (null, "oldText and newText are the same");
        }
        var occurrences = Suggestion.CountOccurrences(content, oldText);
        if (occurrences == 0)
        {
            return (null, "oldText not found in the content (must match exactly, including Markdown markup, spaces and line breaks)");
        }
        if (occurrences > 1)
        {
            return (null, $"oldText occurs {occurrences} times in the content, include more surrounding text to make it unique");
        }
        if (pending.Any(s => s.OldText == oldText && s.NewText == newText))
        {
            return (null, "the same suggestion is already pending");
        }

        return (new Suggestion
        {
            Kind = kind,
            OldText = oldText,
            NewText = newText,
            Comment = (input.Comment ?? "").Trim(),
            Sources = String.Join("\n", (input.Sources ?? []).Select(s => s.Trim()).Where(s => s.Length > 0)),
            Status = SuggestionStatus.Pending,
            CreatedAt = DateTime.UtcNow,
        }, null);
    }

    public async Task<List<Suggestion>> GetPendingAsync(string targetType, int targetId) =>
        await dbContext.Suggestions
            .Where(s => s.TargetType == targetType && s.TargetId == targetId && s.Status == SuggestionStatus.Pending)
            .OrderBy(s => s.Id)
            .ToListAsync();

    /// <summary>
    /// Number of pending suggestions by target id.
    /// </summary>
    public async Task<Dictionary<int, int>> GetPendingCountsAsync(string targetType) =>
        await dbContext.Suggestions
            .Where(s => s.TargetType == targetType && s.Status == SuggestionStatus.Pending)
            .GroupBy(s => s.TargetId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Key, g => g.Count);

    /// <summary>
    /// Marks pending suggestions of the target as accepted, without saving: this is saved (or not) together with
    /// the content they were applied to.
    /// </summary>
    public async Task MarkAcceptedAsync(string targetType, int targetId, IList<int> ids)
    {
        if (ids.Count == 0)
        {
            return;
        }

        var suggestions = await dbContext.Suggestions
            .Where(s => s.TargetType == targetType && s.TargetId == targetId &&
                        s.Status == SuggestionStatus.Pending && ids.Contains(s.Id))
            .ToListAsync();
        foreach (var suggestion in suggestions)
        {
            suggestion.Status = SuggestionStatus.Accepted;
            suggestion.ResolvedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Id of the next post/article/book of the same type (by id, wrapping around) with pending suggestions, if any.
    /// </summary>
    public async Task<int?> GetNextTargetWithPendingAsync(string targetType, int targetId)
    {
        var ids = dbContext.Suggestions
            .Where(s => s.TargetType == targetType && s.TargetId != targetId && s.Status == SuggestionStatus.Pending)
            .Select(s => s.TargetId);
        var next = await ids.Where(id => id > targetId).OrderBy(id => id).Cast<int?>().FirstOrDefaultAsync();
        return next ?? await ids.OrderBy(id => id).Cast<int?>().FirstOrDefaultAsync();
    }

    /// <summary>
    /// Removes all suggestions of a deleted post/article/book, without saving.
    /// </summary>
    public async Task RemoveForTargetAsync(string targetType, int targetId)
    {
        dbContext.Suggestions.RemoveRange(await dbContext.Suggestions
            .Where(s => s.TargetType == targetType && s.TargetId == targetId)
            .ToListAsync());
    }
}
