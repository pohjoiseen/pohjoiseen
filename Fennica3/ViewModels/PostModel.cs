using Holvi.Models;

namespace Fennica3.ViewModels;

public class PostModel
{
    public required Post Post { get; set; }
    public Post? PrevPost { get; set; }
    public Post? NextPost { get; set; }
    public required LayoutParams LayoutParams { get; set; }
}