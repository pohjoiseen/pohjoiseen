using Holvi;
using Microsoft.AspNetCore.Mvc;

namespace KoTi.Controllers.App;

[Route("app/[controller]")]
public class PostsController(HolviDbContext dbContext) : Controller
{
    [HttpGet("Picker/{componentId}")]
    public async Task<IActionResult> Picker(
        string componentId,
        [FromQuery] int limit,
        [FromQuery] int offset,
        [FromQuery] string? postSearch)
    {
        return ViewComponent("PostsPicker", new { componentId, limit, offset, postSearch });
    }
}