namespace Holvi.Models;

public class PlaceLocalization
{
    public int Id { get; set; }
    public int PlaceId { get; set; }
    public Place Place { get; set; } = null!;
    public string Language { get; set; } = "";
    public string Title { get; set; } = "";
    public string Subtitle { get; set; } = "";
    public string Description { get; set; } = "";
    public string ContentMD { get; set; } = "";
    public string Links { get; set; } = "";
    public PlaceMeta? Meta { get; set; }
    public bool Draft { get; set; }
    public DateTime UpdatedAt { get; set; }
}