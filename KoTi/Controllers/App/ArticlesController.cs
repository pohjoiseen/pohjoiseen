using Holvi;
using Holvi.Models;
using Koti.Controllers.App;
using KoTi.ModelFactories;
using KoTi.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace KoTi.Controllers.App;

[Route("app/[controller]")]
public class ArticlesController(ArticleViewModelFactory modelFactory, HolviDbContext dbContext)
    : AbstractContentController<Article, ArticleViewModel, ArticleFormViewComponent>(modelFactory)
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
    public async Task<IActionResult> List(
        string componentId,
        string language,
        [FromQuery] int limit,
        [FromQuery] int offset,
        [FromQuery] string? articleSearch,
        [FromQuery] bool? linkOnly)
    {
        return ViewComponent("ArticleList", new { componentId, language, limit, offset, articleSearch, linkOnly });
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
            // create article
            var article = new Article
            {
                Language = model.Language,
                Name = model.Name,
                Title = model.Title,
                Draft = true,
            };
            dbContext.Articles.Add(article);
            await dbContext.SaveChangesAsync();

            Response.Headers.Append("HX-Redirect", Url.Action("Edit", new { id = article.Id, language = article.Language }));
        }
 
        // the view renders a <dialog>, make sure it is opened
        Response.Headers.Append("HX-Trigger-After-Swap", "{\"dialogopenmodal\":{\"target\":\"#create-article-dialog\"}}");

        return View("_Create", model);
    }

    [HttpDelete("{id}/{language}")]
    public async Task<IActionResult> Delete(int id, string language)
    {
        var article = await dbContext.Articles.FindAsync(id);
        if (article == null)
        {
            return NotFound();
        }
        
        dbContext.Articles.Remove(article);
        await dbContext.SaveChangesAsync();
        
        Response.Headers.Append("HX-Redirect", Url.Action("Index", new { language }));
        return NoContent();
    }
}