using System.ComponentModel.DataAnnotations;
using Holvi;
using Holvi.Models;

namespace KoTi.RequestModels;

public class PlaceRequestDTO
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
    
    [Required]
    public bool IsPrivate { get; set; }
    
    [Required]
    public int Rating { get; set; }
    
    public IList<TagDTO> Tags { get; set; } = [];

    public void ToModel(Place place, HolviDbContext dbContext)
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
        place.Rating = Rating;
        
        foreach (var tagDto in Tags)
        {
            if (place.Tags.SingleOrDefault(t => t.Id == tagDto.Id) == null)
            {
                var newTag = new Tag
                {
                    Id = tagDto.Id.GetValueOrDefault(),
                    Name = tagDto.Name,
                    IsPrivate = tagDto.IsPrivate
                };
                if (tagDto.Id != null)
                {
                    dbContext.Attach(newTag);
                }
                else
                {
                    dbContext.Add(newTag);
                }

                place.Tags.Add(newTag);
            } 
        }

        place.Tags = place.Tags.Where(tagExisting =>
            tagExisting.Id == 0 || Tags.SingleOrDefault(t => t.Id == tagExisting.Id) != null).ToList();
    }
}