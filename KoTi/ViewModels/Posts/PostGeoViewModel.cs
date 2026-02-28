using Holvi.Models;

namespace KoTi.ViewModels.Posts;

public class PostGeoViewModel
{
    public Post.GeoPoint Geo { get; set; } = null!;
    public int Index { get; set; }
}