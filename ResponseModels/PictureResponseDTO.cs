using KoTi.Models;

namespace KoTi.ResponseModels;

public class PictureResponseDTO
{
    public int Id { get; set; }
    public string Filename { get; set; } = "";
    public string Hash { get; set; } = "";
    public string Url { get; set; } = "";
    public string ThumbnailUrl { get; set; } = "";
    public string DetailsUrl { get; set; } = "";
    public DateTime UploadedAt { get; set; }
    public int? PlaceId { get; set; }
    public string? PlaceName { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int Size { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime? PhotographedAt { get; set; }
    public string? Camera { get; set; }
    public string? Lens { get; set; }
    public double? Lat { get; set; }
    public double? Lng { get; set; }
    public bool IsPrivate { get; set; }
    public int Rating { get; set; }
    public int? SetId { get; set; }
    public string? SetName { get; set; }
    public DateTime UpdatedAt { get; set; }

    public static PictureResponseDTO FromModel(Picture picture)
    {
        var dto = new PictureResponseDTO
        {
            Id = picture.Id,
            Filename = picture.Filename,
            Hash = picture.Hash,
            Url = picture.Url,
            ThumbnailUrl = picture.ThumbnailUrl,
            DetailsUrl = picture.DetailsUrl,
            UploadedAt = picture.UploadedAt,
            PlaceId = picture.PlaceId,
            PlaceName = picture.Place?.Name,
            Width = picture.Width,
            Height = picture.Height,
            Size = picture.Size,
            Title = picture.Title,
            Description = picture.Description,
            PhotographedAt = picture.PhotographedAt,
            Camera = picture.Camera,
            Lens = picture.Lens,
            Lat = picture.Lat,
            Lng = picture.Lng,
            IsPrivate = picture.IsPrivate,
            Rating = picture.Rating,
            SetId = picture.SetId,
            SetName = picture.Set?.Name,
            UpdatedAt = picture.UpdatedAt,
        };
        return dto;
    }
}