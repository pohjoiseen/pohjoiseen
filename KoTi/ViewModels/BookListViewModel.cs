using Holvi.Models;

namespace KoTi.ViewModels;

public class BookListViewModel : PaginatedViewModel
{
    public required string ComponentId { get; set; }
    public required string Language { get; set; }
    public required IList<Book> Books { get; set; }
    public string? BookSearchQuery { get; set; }
    public bool LinkOnly { get; set; }
}