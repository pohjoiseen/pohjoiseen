using Holvi.Models;

namespace KoTi.ViewModels;

public class PostListViewModel : PaginatedViewModel
{
    public required string ComponentId { get; set; }
    public required string Language { get; set; }
    public required IList<Post> Posts { get; set; }
    public string? PostSearchQuery { get; set; }
}