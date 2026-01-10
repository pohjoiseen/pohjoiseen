using Holvi;
using KoTi.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace KoTi.Controllers.App;

public class PicturePickerViewComponent(HolviDbContext dbContext) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(string componentId, string fieldName, int? pictureId)
    {
        var picture = pictureId != null ? await dbContext.Pictures.FindAsync(pictureId) : null;
        return View("~/Views/Pictures/_Picker.cshtml", 
            new PicturePickerViewModel { ComponentId = componentId, FieldName = fieldName, Picture = picture });
    }
}