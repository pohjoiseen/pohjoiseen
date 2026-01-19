using Holvi.Models;

namespace KoTi.ViewModels;

public class PlaceViewModel : AbstractContentViewModel
{
    public override string KindLink => "places";
    public override string KindPlural => "Places";
    public IList<string> ParentsTitles { get; init; } = [];
    public IList<int> ParentsIds { get; init; } = [];
    public bool IsLeaf { get; set; }
    public string Name { get; set; } = "";
    public string? Subtitle { get; set; } = "";
    public string Icon { get; set; } = "";
    public string? Description { get; set; } = "";
    public IList<string> Links { get; set; } = [];
    public int? TitlePictureId { get; set; }
    public int? TitleImageOffsetY { get; set; }
    public PlaceMeta Meta { get; set; } = new ();
    public int ExploreStatus { get; set; }
    public double Lat { get; set; }
    public double Lng { get; set; }
    public int Zoom { get; set; }
    public int Rating { get; set; }

    public IList<PlaceChild> Children { get; set; } = [];
    
    public override string FennicaURL => $"/{Language}/places/{Name}/";

    public record PlaceChild
    {
        public int Id { get; set; }
        public bool IsLeaf { get; set; }
        public string Icon { get; set; } = "";
        public string Title { get; set; } = "";
        public bool Draft { get; set; }
        public bool LanguageExists { get; set; }
    }
}