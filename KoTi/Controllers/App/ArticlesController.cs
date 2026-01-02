using Holvi.Models;
using Koti.Controllers.App;
using KoTi.ModelFactories;
using KoTi.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace KoTi.Controllers.App;

[Route("app/[controller]")]
public class ArticlesController(ArticleViewModelFactory modelFactory)
    : AbstractContentController<Article, ArticleViewModel, ArticleFormViewComponent>(modelFactory)
{
    [HttpGet("List/{componentId}/{language}")]
    public async Task<IActionResult> List(
        string componentId,
        string language,
        [FromQuery] int limit,
        [FromQuery] int offset,
        [FromQuery] string? articleSearch)
    {
        return ViewComponent("ArticleList", new { componentId, language, limit, offset, articleSearch });
    }
}