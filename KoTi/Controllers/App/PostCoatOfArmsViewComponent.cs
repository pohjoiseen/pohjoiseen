using Holvi;
using Holvi.Models;
using KoTi.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KoTi.Controllers.App;

public class PostCoatOfArmsViewComponent(HolviDbContext dbContext) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(int index, string url, int? size)
    {
        Picture? picture = null;
        if (int.TryParse(url.TrimStart("picture:"), out var imageId))
        {
            picture = await dbContext.Pictures.FirstOrDefaultAsync(p => p.Id == imageId);
        }
        
        var model = new PostCoatOfArmsViewModel
        {
            Index = index,
            ImageId = imageId,
            Picture = picture,
            Size = size
        };
        
        return View("~/Views/Posts/_CoatOfArmsForm.cshtml", model);
    }
}
