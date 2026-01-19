using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace Holvi.Models;

[Index(nameof(Name))]
[Index(nameof(Icon))]
[Index(nameof(ExploreStatus))]
[Index(nameof(Draft))]
[Index(nameof(Rating))]
public class Place : IContentEntity
{
    public int Id { get; set; }
    
    public int? ParentId { get; set; }

    [JsonIgnore] public Place? Parent { get; set; } = null!;
    
    public bool IsLeaf { get; set; }
    
    [MaxLength(80)]
    public string Name { get; set; } = "";

    public string? Title { get; set; } = "";

    public string? Subtitle { get; set; } = "";

    public string? Icon { get; set; } = "";
    
    public string? Description { get; set; } = "";
    
    public string? ContentMD { get; set; } = "";

    public string? Links { get; set; } = "";

    public int? TitlePictureId { get; set; }
    public int? TitleImageOffsetY { get; set; }
    
    public PlaceMeta? Meta { get; set; }
    
    public ExploreStatus ExploreStatus { get; set; }
    
    public int Order { get; set; }
    
    public double Lat { get; set; }
    
    public double Lng { get; set; }
    
    public int Zoom { get; set; }
    
    public bool Draft { get; set; }
    
    public int Rating { get; set; }
    
    public DateTime UpdatedAt { get; set; }

    public IList<Picture> Pictures { get; } = [];

    public IList<Tag> Tags { get; set; } = [];
    
    public IList<PlaceLocalization> Localizations { get; set; } = [];
    
    public IList<Place> Children { get; set; } = [];
}