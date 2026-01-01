using Holvi.Models;
using Koti.Controllers.App;
using KoTi.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace KoTi.Controllers.App;

[Route("app/[controller]")]
public class ArticlesController(ArticleViewModelFactory modelFactory)
    : AbstractContentController<Article, ArticleViewModel, ArticleFormViewComponent>(modelFactory)
{
    [HttpGet("Picker/{componentId}")]
    public async Task<IActionResult> Picker(
        string componentId,
        [FromQuery] int limit,
        [FromQuery] int offset,
        [FromQuery] string? articleSearch)
    {
        return ViewComponent("ArticlesPicker", new { componentId, limit, offset, articleSearch });
    }
}