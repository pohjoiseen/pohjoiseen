using System.ComponentModel.DataAnnotations;
using KoTi.Models;

namespace KoTi.RequestModels;

public class PictureDTO
{
    [Required] public string Filename { get; set; } = "";
    [Required] public string Hash { get; set; } = "";
    [Required] public string Url { get; set; } = "";
    [Required] public string ThumbnailUrl { get; set; } = "";
    [Required] public string DetailsUrl { get; set; } = "";
    [Required] public DateTime UploadedAt { get; set; }
    public int? PlaceId { get; set; }
    [Required] public int Width { get; set; }
    [Required] public int Height { get; set; }
    [Required] public int Size { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime? PhotographedAt { get; set; }
    public string? Camera { get; set; }
    public string? Lens { get; set; }
    public double? Lat { get; set; }
    public double? Lng { get; set; }

    public void ToModel(Picture picture)
    {
        picture.Filename = Filename;
        picture.Hash = Hash;
        picture.Url = Url;
        picture.ThumbnailUrl = ThumbnailUrl;
        picture.DetailsUrl = DetailsUrl;
        picture.UploadedAt = UploadedAt;
        picture.PlaceId = PlaceId;
        picture.Width = Width;
        picture.Height = Height;
        picture.Size = Size;
        picture.Title = Title;
        picture.Description = Description;
        picture.PhotographedAt = PhotographedAt;
        picture.Camera = Camera;
        picture.Lens = Lens;
        picture.Lat = Lat;
        picture.Lng = Lng;
    }
}