using Holvi.Models;

namespace KoTi.ViewModels.Posts;

public class PostListViewModel : PaginatedViewModel
{
    public required string ComponentId { get; set; }
    public required string Language { get; set; }
    public required IList<Post> Posts { get; set; }
    public string? PostSearchQuery { get; set; }
    public bool LinkOnly { get; set; }

    /// <summary>
    /// Number of pending suggestions by id, for those which have any.
    /// </summary>
    public IDictionary<int, int> PendingSuggestions { get; set; } = new Dictionary<int, int>();
}