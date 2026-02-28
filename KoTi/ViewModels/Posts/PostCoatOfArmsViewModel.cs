using Holvi.Models;

namespace KoTi.ViewModels.Posts;

public class PostCoatOfArmsViewModel
{
    public int Index { get; set; }
    public int ImageId { get; set; }
    public Picture? Picture { get; set; }
    public int? Size { get; set; }
}
