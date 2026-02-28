using Holvi.Models;

namespace KoTi.ViewModels.Articles;

public class ArticleListViewModel : PaginatedViewModel
{
    public required string ComponentId { get; set; }
    public required string Language { get; set; }
    public required IList<Article> Articles { get; set; }
    public string? ArticleSearchQuery { get; set; }
    public bool LinkOnly { get; set; }
}