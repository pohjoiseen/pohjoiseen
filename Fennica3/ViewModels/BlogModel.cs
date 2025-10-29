using Holvi.Models;

namespace Fennica3.ViewModels;

public class BlogModel
{
    public required IEnumerable<Post> Posts { get; set; }
    public required LayoutParams LayoutParams { get; set; }
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; }
    public required IList<int> Pagination { get; set; }
    public string? IntroText { get; set; }
    public string? MapAsideText { get; set; }
}