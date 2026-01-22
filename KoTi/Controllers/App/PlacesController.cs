using Holvi;
using Holvi.Models;
using Koti.Controllers.App;
using KoTi.ModelFactories;
using KoTi.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KoTi.Controllers.App;

[Route("app/[controller]")]
public class PlacesController(HolviDbContext dbContext, PlaceViewModelFactory modelFactory)
    : AbstractContentController<Place, PlaceViewModel, PlaceFormViewComponent>(modelFactory)
{
    [HttpGet("Create/{parentId:int}/{language}")]
    [HttpPost("Create/{parentId:int}/{language}")]
    public async Task<IActionResult> Create(CreatePlaceViewModel model, string language, int parentId)
    {
        var parent = await dbContext.Places.FindAsync(parentId);
        if (parent is null)
        {
            return NotFound();
        }
        model.ParentId = parentId;
        model.Language = language;
        
        // default to parent coordinates
        if (model is { Lat: 0, Lng: 0 })
        {
            model.Lat = parent.Lat;
            model.Lng = parent.Lng;
        }

        // do not validate on GET
        if (Request.Method == "GET")
        {
            ModelState.Clear();
        }
        
        if (Request.Method == "POST" && ModelState.IsValid)
        {
            // create place and optionally localization
            var place = new Place
            {
                Lat = model.Lat,
                Lng = model.Lng,
                ParentId = model.ParentId,
                IsLeaf = model.IsLeaf,
                Name = model.Name,
                Icon = "star",
                Order = await dbContext.Places.Where(p => p.ParentId == model.ParentId).CountAsync() + 1
            };
            dbContext.Places.Add(place);
            if (model.Language == "en")
            {
                place.Title = model.Title;
            }
            else
            {
                var localization = new PlaceLocalization
                {
                    Place = place,
                    Language = model.Language,
                    Title = model.Title
                };
                dbContext.PlaceLocalizations.Add(localization);
            }
            await dbContext.SaveChangesAsync();

            // append entry to list
            return View("CreateSuccess", (place.Order - 1, model.Language, new PlaceViewModel.PlaceChild
            {
                Id = place.Id,
                Draft = place.Draft,
                Icon = place.Icon,
                IsLeaf = place.IsLeaf,
                LanguageExists = true,
                Title = model.Title
            }));
        }
 
        // the view renders a <dialog>, make sure it is opened
        Response.Headers.Append("HX-Trigger-After-Swap", "{\"dialogopenmodal\":{\"target\":\"#create-place-dialog\"}}");

        return View("~/Views/Places/Create.cshtml", model);
    }
}