using Holvi;
using Holvi.Models;
using Koti.Controllers.App;
using KoTi.ModelFactories;
using KoTi.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace KoTi.Controllers.App;

[Route("app/[controller]")]
public class PostsController(PostViewModelFactory modelFactory, HolviDbContext dbContext)
    : AbstractContentController<Post, PostViewModel, PostFormViewComponent>(modelFactory)
{
    [HttpGet]
    public IActionResult Index()
    {
        return RedirectToAction("Index", new { language = "ru" });
    }
    
    [HttpGet("{language:length(2)}")]
    public IActionResult Index(string language)
    {
        ViewData["Language"] = language;
        return View("Index");
    }
    
    [HttpGet("List/{componentId}/{language}")]
    public IActionResult List(
        string componentId,
        string language,
        [FromQuery] int limit,
        [FromQuery] int offset,
        [FromQuery] string? postSearch,
        [FromQuery] bool? linkOnly)
    {
        return ViewComponent("PostList", new { componentId, language, limit, offset, postSearch, linkOnly });
    }
    
    [HttpGet("CoatOfArms")]
    public IActionResult CoatOfArms(int index, string url, int? size)
    {
        return ViewComponent("PostCoatOfArms", new { index, url, size });
    }
    
    [HttpGet("Geo")]
    public IActionResult Geo(int index)
    {
        return ViewComponent("PostGeo", new { index });
    }
    
    [HttpGet("Create/{language}")]
    [HttpPost("Create/{language}")]
    public async Task<IActionResult> Create(CreateContentViewModel model, string language)
    {
        model.Language = language;
        
        // do not validate on GET
        if (Request.Method == "GET")
        {
            ModelState.Clear();
        }
        
        if (Request.Method == "POST" && ModelState.IsValid)
        {
            // create post
            var post = new Post
            {
                Language = model.Language,
                Name = model.Name,
                Date = DateOnly.FromDateTime(DateTime.Now),
                Title = model.Title,
                Draft = true,
            };
            dbContext.Posts.Add(post);
            await dbContext.SaveChangesAsync();

            Response.Headers.Append("HX-Redirect", Url.Action("Edit", new { id = post.Id, language = post.Language }));
        }
 
        // the view renders a <dialog>, make sure it is opened
        Response.Headers.Append("HX-Trigger-After-Swap", "{\"dialogopenmodal\":{\"target\":\"#create-post-dialog\"}}");

        return View("_Create", model);
    }

    [HttpDelete("{id}/{language}")]
    public async Task<IActionResult> Delete(int id, string language)
    {
        var posts = await dbContext.Posts.FindAsync(id);
        if (posts == null)
        {
            return NotFound();
        }
        
        dbContext.Posts.Remove(posts);
        await dbContext.SaveChangesAsync();
        
        Response.Headers.Append("HX-Redirect", Url.Action("Index", new { language }));
        return NoContent();
    }
}