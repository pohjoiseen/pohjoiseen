namespace Holvi.Models;

public class PlaceLocalization
{
    public int Id { get; set; }
    public int PlaceId { get; set; }
    public Place Place { get; set; } = null!;
    public string Language { get; set; } = "";
    public string Name { get; set; } = "";
    public string Alias { get; set; } = "";
    public string Notes { get; set; } = "";
    public string Links { get; set; } = "";
    public string Directions { get; set; } = "";
    public string PublicTransport { get; set; } = "";
    public string Season { get; set; } = "";
    public int Order { get; set; }
    public bool IsPrivate { get; set; }
    public DateTime UpdatedAt { get; set; }
}