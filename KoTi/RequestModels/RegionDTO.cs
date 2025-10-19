using System.ComponentModel.DataAnnotations;
using KoTi.Models;

namespace KoTi.RequestModels;

public class RegionDTO
{
    [Required] public string Name { get; set; } = "";

    [Required]
    public int CountryId { get; set; }
    
    [Required]
    public int Order { get; set; }
    
    public void ToModel(Region region)
    {
        region.Name = Name;
        region.CountryId = CountryId;
        region.Order = Order;
    }
}
