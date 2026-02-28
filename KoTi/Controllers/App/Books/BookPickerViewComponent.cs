using Holvi;
using KoTi.ViewModels.Books;
using Microsoft.AspNetCore.Mvc;

namespace KoTi.Controllers.App.Books;

public class BookPickerViewComponent(HolviDbContext dbContext) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(string componentId, string fieldName, string language, int? bookId)
    {
        var book = bookId != null ? await dbContext.Books.FindAsync(bookId) : null;
        return View("~/Views/Books/_Picker.cshtml", 
            new BookPickerViewModel { ComponentId = componentId, FieldName = fieldName, Book = book, Language = language });
    }
}