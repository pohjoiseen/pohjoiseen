using Holvi;
using Microsoft.AspNetCore.Mvc;

namespace KoTi.Controllers.App;

public class PlaceMetaCoatOfArmsViewComponent(HolviDbContext dbContext) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(int? pictureId)
    {
        var picture = pictureId is not null
            ? await dbContext.Pictures.FindAsync(pictureId)
            : null;
        
        return View("~/Views/Places/_MetaCoatOfArms.cshtml", picture);
    }
}