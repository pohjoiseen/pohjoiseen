using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using KoTi.Models;

namespace KoTi.RequestModels;

public class PlaceDTO
{
    [Required] public string Name { get; set; } = "";

    public string Alias { get; set; } = "";
    
    [Required]
    public int AreaId { get; set; }

    public string Notes { get; set; } = "";

    [Required]
    public string Category { get; set; } = "";

    public string Links { get; set; } = "";
    
    public string Directions { get; set; } = "";

    public string PublicTransport { get; set; } = "";

    public string Season { get; set; } = "";

    [Required]
    public ExploreStatus ExploreStatus { get; set; }
    
    [Required]
    public int Order { get; set; }
    
    public double Lat { get; set; }
    
    public double Lng { get; set; }
    
    public int Zoom { get; set; }
    
    public bool IsPrivate { get; set; }

    public void ToModel(Place place)
    {
        place.Name = Name;
        place.Alias = Alias;
        place.AreaId = AreaId;
        place.Category = Category;
        place.Notes = Notes;
        place.Links = Links;
        place.Directions = Directions;
        place.PublicTransport = PublicTransport;
        place.Season = Season;
        place.ExploreStatus = ExploreStatus;
        place.Order = Order;
        place.Lat = Lat;
        place.Lng = Lng;
        place.Zoom = Zoom;
        place.IsPrivate = IsPrivate;
    }
}