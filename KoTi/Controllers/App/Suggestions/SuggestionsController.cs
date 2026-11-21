using Holvi;
using Holvi.Models;
using KoTi.ViewModels.Suggestions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KoTi.Controllers.App.Suggestions;

[Route("[controller]")]
public class SuggestionsController(HolviDbContext dbContext) : Controller
{
    /// <summary>
    /// Everything with pending suggestions; reviewing itself happens on the edit pages.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var groups = (await dbContext.Suggestions
                .Where(s => s.Status == SuggestionStatus.Pending)
                .Select(s => new { s.TargetType, s.TargetId, s.Batch })
                .ToListAsync())
            .GroupBy(s => (s.TargetType, s.TargetId))
            .ToList();

        async Task<Dictionary<int, (string Language, string Title, string Order)>> Load(string targetType)
        {
            var ids = groups.Where(g => g.Key.TargetType == targetType).Select(g => g.Key.TargetId).ToList();
            return targetType switch
            {
                Suggestion.Post => (await dbContext.Posts.Where(p => ids.Contains(p.Id)).ToListAsync())
                    .ToDictionary(p => p.Id, p => (p.Language, p.Title, p.ToString())),
                Suggestion.Article => (await dbContext.Articles.Where(a => ids.Contains(a.Id)).ToListAsync())
                    .ToDictionary(a => a.Id, a => (a.Language, a.Title, a.Name)),
                _ => (await dbContext.Books.Where(b => ids.Contains(b.Id)).ToListAsync())
                    .ToDictionary(b => b.Id, b => (b.Language, b.Title, b.Name)),
            };
        }

        var controllers = new Dictionary<string, string>
        {
            [Suggestion.Post] = "Posts", [Suggestion.Article] = "Articles", [Suggestion.Book] = "Books"
        };
        var targets = new List<(string Order, SuggestionsIndexViewModel.Target Target)>();
        foreach (var targetType in Suggestion.TargetTypes)
        {
            var entities = await Load(targetType);
            foreach (var group in groups.Where(g => g.Key.TargetType == targetType))
            {
                // deleted without its suggestions (not from KoTi)
                if (!entities.TryGetValue(group.Key.TargetId, out var entity)) continue;
                targets.Add((entity.Order, new SuggestionsIndexViewModel.Target(
                    targetType, group.Key.TargetId, entity.Language, entity.Title, group.Count(),
                    group.Select(s => s.Batch).Distinct().Order().ToList(),
                    Url.Action("Edit", controllers[targetType], new { id = group.Key.TargetId, language = entity.Language })!)));
            }
        }

        return View("~/Views/Suggestions/Index.cshtml", new SuggestionsIndexViewModel
        {
            // posts by date, articles and books by name, as in their lists
            Targets = targets
                .OrderBy(t => Array.IndexOf(Suggestion.TargetTypes, t.Target.Kind)).ThenBy(t => t.Order, StringComparer.Ordinal)
                .Select(t => t.Target).ToList()
        });
    }
}
