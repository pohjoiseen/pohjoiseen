using Holvi.Models;
using KoTi.ModelFactories;
using KoTi.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        if (languages.ContainsKey("ru"))
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
            try
            {
                var entity = await modelFactory.SaveAsync(id, language, model);
                if (entity is not null)
                {
                    model.Version = entity.Version;
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                // saved from elsewhere after this page was loaded (optimistic locking by version).
                // Render the form with everything as submitted, incl. the old version, so that
                // nothing is lost in the browser but saving from that page keeps failing
                Response.StatusCode = 409;
                ViewData["Conflict"] = true;
            }
        }
        else
        {
            Response.StatusCode = 400;
        }
        
        ViewData["FormViewComponentType"] = typeof(TFormViewComponent);
        return View("~/Views/AbstractContent/_Save.cshtml", model);
    }

    [HttpGet("{id:int}/{language}/copyTo/{targetLanguage}")]
    [HttpPost("{id:int}/{language}/copyTo/{targetLanguage}")]
    public async Task<IActionResult> CopyToLanguage(int id, string language, string targetLanguage)
    {
        ViewData["TargetLanguage"] = targetLanguage;
        var model = await modelFactory.LoadAsync(id, language);
        if (model is null)
        {
            return NotFound();
        }

        if (Request.Method == "POST")
        {
            var newEntityId = await modelFactory.CopyToLanguageAsync(id, language, targetLanguage);
            return RedirectToAction("Edit", new { id = newEntityId, language = targetLanguage });
        }

        //await modelFactory.SaveAsync(id, targetLanguage, model);
        //return RedirectToAction("Edit", new {id, language = targetLanguage});
        return View("~/Views/AbstractContent/CopyToLanguage.cshtml", model);
    }
}