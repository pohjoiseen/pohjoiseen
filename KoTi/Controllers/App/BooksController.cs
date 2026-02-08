using Holvi;
using Holvi.Models;
using Koti.Controllers.App;
using KoTi.ModelFactories;
using KoTi.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace KoTi.Controllers.App;

[Route("app/[controller]")]
public class BooksController(BookViewModelFactory modelFactory, HolviDbContext dbContext)
    : AbstractContentController<Book, BookViewModel, BookFormViewComponent>(modelFactory)
{
    [HttpGet]
    public IActionResult Index()
    {
        return RedirectToAction("Index", new { language = "ru" });
    }
    
    [HttpGet("{language:length(2)}")]
    public IActionResult Index(string language)
    {
        ViewData["Language"] = language;
        return View("Index");
    }
    
    [HttpGet("List/{componentId}/{language}")]
    public async Task<IActionResult> List(
        string componentId,
        string language,
        [FromQuery] int limit,
        [FromQuery] int offset,
        [FromQuery] string? bookSearch,
        [FromQuery] bool? linkOnly)
    {
        return ViewComponent("BookList", new { componentId, language, limit, offset, bookSearch, linkOnly });
    }
    
    [HttpGet("Picker/{componentId}/{language}/{fieldName}")]
    public async Task<IActionResult> Picker(string componentId, string fieldName, string language, int? bookId)
    {
        return ViewComponent("BookPicker", new { componentId, fieldName, bookId, language });
    }
    
    [HttpGet("Create/{language}")]
    [HttpPost("Create/{language}")]
    public async Task<IActionResult> Create(CreateContentViewModel model, string language)
    {
        model.Language = language;
        
        // do not validate on GET
        if (Request.Method == "GET")
        {
            ModelState.Clear();
        }
        
        if (Request.Method == "POST" && ModelState.IsValid)
        {
            // create book
            var book = new Book
            {
                Language = model.Language,
                Name = model.Name,
                Title = model.Title,
                Draft = true,
            };
            dbContext.Books.Add(book);
            await dbContext.SaveChangesAsync();

            Response.Headers.Append("HX-Redirect", Url.Action("Edit", new { id = book.Id, language = book.Language }));
        }
 
        // the view renders a <dialog>, make sure it is opened
        Response.Headers.Append("HX-Trigger-After-Swap", "{\"dialogopenmodal\":{\"target\":\"#create-book-dialog\"}}");

        return View("_Create", model);
    }

    [HttpDelete("{id}/{language}")]
    public async Task<IActionResult> Delete(int id, string language)
    {
        var book = await dbContext.Books.FindAsync(id);
        if (book == null)
        {
            return NotFound();
        }
        
        dbContext.Books.Remove(book);
        await dbContext.SaveChangesAsync();
        
        Response.Headers.Append("HX-Redirect", Url.Action("Index", new { language }));
        return NoContent();
    }
}