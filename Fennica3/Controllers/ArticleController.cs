using Fennica3.ViewModels;
using Holvi;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fennica3.Controllers;

public class ArticleController(HolviDbContext dbContext, IConfiguration configuration) : Controller
{
    [HttpGet("/{language}/article/{name}")]
    public async Task<IActionResult> View(string language, string name)
    {
        // articles starting with underscore are for internal use only
        if (name[0] == '_') return NotFound();

        var article = await dbContext.Articles
            .Where(a => a.Language == language)
            .Where(a => a.Name == name)
            .Where(a => configuration["Fennica3:WithDrafts"] != null || !a.Draft)
            .FirstOrDefaultAsync();
        if (article == null) return NotFound();

        var layoutParams = new LayoutParams
        {
            Title = article.Title,
            Language = article.Language,
            BodyClass = "body-article",
        };
        if (article.Name == "about") layoutParams.BigHeaderPage = "about";
        if (article.Name == "contents") layoutParams.BigHeaderPage = "contents";

        return View("View", new ArticleModel { Article = article, LayoutParams = layoutParams });
    }
}