using KoTi.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace KoTi.Controllers.App;

public class PostFormViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(PostViewModel model)
    {
        return View("~/Views/Posts/_Form.cshtml", model);
    }
}