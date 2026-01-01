using Holvi.Models;

namespace KoTi.ViewModels;

public class PostPickerViewModel : PaginatedViewModel
{
    public required string ComponentId { get; set; }
    public required IList<Post> Posts { get; set; }
    public string? PostSearchQuery { get; set; }
}