using Holvi.Models;

namespace Fennica3.ResponseModels;

public record PostDefinitionForMap
{
    public int Id { get; init; }
    public required string Title { get; init; }
    public string TitleImage { get; init; } = "";
    public string Description { get; init; } = "";
    public bool Mini { get; init; }
    public IList<Post.GeoPoint> Geo { get; init; } = new List<Post.GeoPoint>();
}
