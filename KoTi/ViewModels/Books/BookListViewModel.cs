using Holvi.Models;

namespace KoTi.ViewModels.Books;

public class BookListViewModel : PaginatedViewModel
{
    public required string ComponentId { get; set; }
    public required string Language { get; set; }
    public required IList<Book> Books { get; set; }
    public string? BookSearchQuery { get; set; }
    public bool LinkOnly { get; set; }

    /// <summary>
    /// Number of pending suggestions by id, for those which have any.
    /// </summary>
    public IDictionary<int, int> PendingSuggestions { get; set; } = new Dictionary<int, int>();
}