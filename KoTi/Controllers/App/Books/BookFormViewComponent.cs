using KoTi.ViewModels.Books;
using Microsoft.AspNetCore.Mvc;

namespace KoTi.Controllers.App.Books;

public class BookFormViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(BookViewModel model)
    {
        return View("~/Views/Books/_Form.cshtml");
    }
}