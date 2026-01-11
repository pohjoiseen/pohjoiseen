using Holvi.Models;
using Koti.Controllers.App;
using KoTi.ModelFactories;
using KoTi.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace KoTi.Controllers.App;

[Route("app/[controller]")]
public class PostsController(PostViewModelFactory modelFactory)
    : AbstractContentController<Post, PostViewModel, PostFormViewComponent>(modelFactory)
{
    [HttpGet("List/{componentId}/{language}")]
    public async Task<IActionResult> List(
        string componentId,
        string language,
        [FromQuery] int limit,
        [FromQuery] int offset,
        [FromQuery] string? postSearch)
    {
        return ViewComponent("PostList", new { componentId, language, limit, offset, postSearch });
    }
    
    [HttpGet("CoatOfArms")]
    public async Task<IActionResult> CoatOfArms(int index, string url, int? size)
    {
        return ViewComponent("PostCoatOfArms", new { index, url, size });
    }
    
}