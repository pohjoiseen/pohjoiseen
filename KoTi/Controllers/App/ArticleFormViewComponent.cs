using KoTi.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace KoTi.Controllers.App;

public class ArticleFormViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(ArticleViewModel model)
    {
        return View("~/Views/Articles/_Form.cshtml");
    }
}