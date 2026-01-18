using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace Holvi.Models;

// note that field naming is fairly inconsistent between areas/places and posts/articles,
// since these were originally created for entirely different purposes
[Index(nameof(Slug))]
[Index(nameof(Category))]
[Index(nameof(ExploreStatus))]
[Index(nameof(IsPrivate))]
[Index(nameof(Rating))]
public class Place
{
    public int Id { get; set; }
    
    public int AreaId { get; set; }

    [JsonIgnore] public Area Area { get; set; } = null!;
    
    public string Slug { get; set; } = "";

    public string Name { get; set; } = "";

    public string Alias { get; set; } = "";

    public string Category { get; set; } = "";
    
    public string Notes { get; set; } = "";

    public string Links { get; set; } = "";

    public string Directions { get; set; } = "";

    public string PublicTransport { get; set; } = "";

    public string Season { get; set; } = "";
    
    public ExploreStatus ExploreStatus { get; set; }
    
    public int Order { get; set; }
    
    public double Lat { get; set; }
    
    public double Lng { get; set; }
    
    public int Zoom { get; set; }
    
    public bool IsPrivate { get; set; }
    
    public int Rating { get; set; }
    
    public DateTime UpdatedAt { get; set; }

    public IList<Picture> Pictures { get; } = [];

    public IList<Tag> Tags { get; set; } = [];
    
    public IList<PlaceLocalization> Localizations { get; set; } = [];
}