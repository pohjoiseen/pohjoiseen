using Holvi.Models;
using Koti.Controllers.App;
using KoTi.ModelFactories;
using KoTi.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace KoTi.Controllers.App;

[Route("app/[controller]")]
public class PlacesController(PlaceViewModelFactory modelFactory)
    : AbstractContentController<Place, PlaceViewModel, PlaceFormViewComponent>(modelFactory)
{
}