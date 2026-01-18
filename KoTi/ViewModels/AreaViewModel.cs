using Holvi.Models;

namespace KoTi.ViewModels;

public class AreaViewModel : AbstractContentViewModel
{
    public override string KindLink => "area";
    public override string KindPlural => "Areas";
    public string ParentsDescription { get; init; } = "";
    public string Slug { get; set; } = "";
    public string? Subtitle { get; set; } = "";
    public IList<string> Links { get; set; } = [];
    public int ExploreStatus { get; set; }
    public double Lat { get; set; }
    public double Lng { get; set; }
    public int Zoom { get; set; }
    
    public IList<PlaceLink> PlaceLinks { get; set; } = [];
    
    public override string FennicaURL => $"/{Language}/areas/{Slug}/";

    public record PlaceLink
    {
        public int Id { get; set; }
        public string Icon { get; set; } = "";
        public string Title { get; set; } = "";
        public bool LanguageExists { get; set; }
    }
}