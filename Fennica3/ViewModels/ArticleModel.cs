using Holvi.Models;

namespace Fennica3.ViewModels;

public class ArticleModel
{
    public required Article Article { get; set; }
    public required LayoutParams LayoutParams { get; set; }
}