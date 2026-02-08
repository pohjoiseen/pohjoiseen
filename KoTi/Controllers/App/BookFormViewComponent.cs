using KoTi.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace KoTi.Controllers.App;

public class BookFormViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(BookViewModel model)
    {
        return View("~/Views/Books/_Form.cshtml");
    }
}