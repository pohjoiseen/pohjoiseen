namespace Holvi.Models;

public class PlaceMeta
{
    public string? Directions { get; set; }
    public string? PublicTransport { get; set; }
    public string? Season { get; set; }

    // hidden
    public string? FlagEmoji { get; set; }
    public string? MapType { get; set; }
}