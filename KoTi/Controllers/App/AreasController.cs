using Holvi.Models;
using Koti.Controllers.App;
using KoTi.ModelFactories;
using KoTi.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace KoTi.Controllers.App;

[Route("app/[controller]")]
public class AreasController(AreaViewModelFactory modelFactory)
    : AbstractContentController<Area, AreaViewModel, AreaFormViewComponent>(modelFactory)
{
}