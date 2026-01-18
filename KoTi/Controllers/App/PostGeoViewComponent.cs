using Holvi.Models;
using KoTi.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace KoTi.Controllers.App;

public class PostGeoViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(int index, Post.GeoPoint? geo)
    {
        var model = new PostGeoViewModel
        {
            Index = index,
            Geo = geo ?? new Post.GeoPoint()
        };
        
        return View("~/Views/Posts/_Geo.cshtml", model);
    }
}