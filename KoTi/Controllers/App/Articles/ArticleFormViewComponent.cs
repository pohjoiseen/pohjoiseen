using KoTi.ViewModels.Articles;
using Microsoft.AspNetCore.Mvc;

namespace KoTi.Controllers.App.Articles;

public class ArticleFormViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(ArticleViewModel model)
    {
        return View("~/Views/Articles/_Form.cshtml");
    }
}