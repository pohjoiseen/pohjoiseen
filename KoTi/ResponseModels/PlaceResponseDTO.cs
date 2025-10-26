using System.ComponentModel.DataAnnotations;
using Holvi.Models;

namespace KoTi.ResponseModels;

public class PlaceResponseDTO
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Alias { get; set; } = "";
    public int AreaId { get; set; }
    public string Notes { get; set; } = "";
    public string Category { get; set; } = "";
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
    public string? ThumbnailUrl { get; set; }

    public IList<Tag> Tags { get; init; } = [];
    
    public static PlaceResponseDTO FromModel(Place place, string? thumbnailUrl)
    {
        var dto = new PlaceResponseDTO
        {
            Id = place.Id,
            Name = place.Name,
            Alias = place.Alias,
            AreaId = place.AreaId,
            Category = place.Category,
            Notes = place.Notes,
            Links = place.Links,
            Directions = place.Directions,
            PublicTransport = place.PublicTransport,
            Season = place.Season,
            ExploreStatus = place.ExploreStatus,
            Order = place.Order,
            Lat = place.Lat,
            Lng = place.Lng,
            Zoom = place.Zoom,
            IsPrivate = place.IsPrivate,
            Rating = place.Rating,
            UpdatedAt = place.UpdatedAt,
            Tags = place.Tags,
            ThumbnailUrl = thumbnailUrl,
        };
        return dto;
    }
}