using System.ComponentModel.DataAnnotations;
using KoTi.Models;

namespace KoTi.RequestModels;

public class AreaDTO
{
    [Required] public string Name { get; set; } = "";

    [Required]
    public int RegionId { get; set; }

    public string Notes { get; set; } = "";

    public string Links { get; set; } = "";
    
    [Required]
    public ExploreStatus ExploreStatus { get; set; }
    
    [Required]
    public int Order { get; set; }
    
    public double Lat { get; set; }
    
    public double Lng { get; set; }
    
    public int Zoom { get; set; }

    public void ToModel(Area area)
    {
        area.Name = Name;
        area.RegionId = RegionId;
        area.Notes = Notes;
        area.Links = Links;
        area.ExploreStatus = ExploreStatus;
        area.Order = Order;
        area.Lat = Lat;
        area.Lng = Lng;
        area.Zoom = Zoom;
    }
}