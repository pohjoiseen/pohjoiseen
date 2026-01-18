using KoTi.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace KoTi.Controllers.App;

public class AreaFormViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(AreaViewModel model)
    {
        return View("~/Views/Areas/_Form.cshtml", model);
    }
}