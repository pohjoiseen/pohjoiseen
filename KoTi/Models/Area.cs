using System.Text.Json.Serialization;

namespace KoTi.Models;

public class Area
{
    public int Id { get; set; }
    
    public int RegionId { get; set; }
    
    [JsonIgnore]
    public Region Region { get; set; }
    
    public string Name { get; set; }
    
    public string Notes { get; set; }
    
    public string Links { get; set; }
    
    public ExploreStatus ExploreStatus { get; set; }
    
    public int Order { get; set; }
    
    public double Lat { get; set; }
    
    public double Lng { get; set; }
    
    public int Zoom { get; set; }
    
    public DateTime UpdatedAt { get; set; }

    public IList<Place> Places { get; set; }
}