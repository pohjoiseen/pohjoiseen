using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace Holvi.Models;

// note that field naming is fairly inconsistent between areas/places and posts/articles,
// since these were originally created for entirely different purposes
[Index(nameof(Slug))]
[Index(nameof(IsPrivate))]
public class Area : IContentEntity
{
    public int Id { get; set; }
    
    public int RegionId { get; set; }

    [JsonIgnore] public Region Region { get; set; } = null!;
    
    public string Slug { get; set; } = "";

    public string Name { get; set; } = "";
    
    public string Alias { get; set; } = "";

    public string Notes { get; set; } = "";

    public string Links { get; set; } = "";
    
    public ExploreStatus ExploreStatus { get; set; }
    
    public int Order { get; set; }
    
    public double Lat { get; set; }
    
    public double Lng { get; set; }
    
    public int Zoom { get; set; }
    
    public DateTime UpdatedAt { get; set; }
    
    public bool IsPrivate { get; set; }

    public IList<Place> Places { get; set; } = [];
    
    public IList<AreaLocalization> Localizations { get; set; } = [];
}