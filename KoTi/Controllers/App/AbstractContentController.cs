using Holvi.Models;
using KoTi.ModelFactories;
using KoTi.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Koti.Controllers.App;

public abstract class AbstractContentController<TEntity, TViewModel, TFormViewComponent>(IContentViewModelFactory<TViewModel, TEntity> modelFactory) : Controller
    where TEntity : IContentEntity where TViewModel : AbstractContentViewModel where TFormViewComponent : ViewComponent
{
    [HttpGet("{id:int}")]
    public async Task<IActionResult> EditAnyLanguage(int id)
    {
        var languages = await modelFactory.GetAllLanguagesAsync(id);
        if (languages.Count == 0)
        {
            return NotFound();
        }
        if (languages.Contains("ru"))
        {
            return RedirectToAction("Edit", new {id, language = "ru"});
        }
        return RedirectToAction("Edit", new {id, language = languages.First()});
    }
    
    [HttpGet("{id:int}/{language}")]
    public async Task<IActionResult> Edit(int id, string language)
    {
        var model = await modelFactory.LoadAsync(id, language);
        
        ViewData["FormViewComponentType"] = typeof(TFormViewComponent);
        return View("~/Views/AbstractContent/Edit.cshtml", model);
    }

    [HttpPut("{id:int}/{language}")]
    public async Task<IActionResult> Save(int id, string language, TViewModel model)
    {
        if (ModelState.IsValid)
        {
            await modelFactory.SaveAsync(id, language, model);
        }
        else
        {
            Response.StatusCode = 400;
        }
        
        ViewData["FormViewComponentType"] = typeof(TFormViewComponent);
        return View("~/Views/AbstractContent/Save.cshtml", model);
    }
}