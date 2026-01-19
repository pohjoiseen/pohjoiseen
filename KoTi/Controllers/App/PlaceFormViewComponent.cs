using KoTi.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace KoTi.Controllers.App;

public class PlaceFormViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(PlaceViewModel model)
    {
        return View("~/Views/Places/_Form.cshtml", model);
    }
}