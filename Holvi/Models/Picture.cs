using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace Holvi.Models;

[Index(nameof(Hash), IsUnique = true)]
[Index(nameof(PhotographedAt))]
[Index(nameof(IsPrivate))]
[Index(nameof(Rating))]
public class Picture
{
    public const int ThumbnailSize = 125;
    public const int DetailsSize = 500;
    public const int WebsiteSize = 677;  // must match ImageSize in FennicaProject
    public const string ThumbnailSuffix = ".t";
    public const string DetailsSuffix = ".d";
    public const string Website1xSuffix = ".1x";
    public const string Website2xSuffix = ".2x";
    
    public int Id { get; set; }

    public string Filename { get; set; } = "";

    public string Hash { get; set; } = "";
    public string Url { get; set; } = "";
    public string ThumbnailUrl { get; set; } = "";
    public string DetailsUrl { get; set; } = "";
    
    public DateTime UploadedAt { get; set; }
    
    public int? PlaceId { get; set; }
    [JsonIgnore]
    public Place? Place { get; set; }
    
    public int Width { get; set; }
    public int Height { get; set; }
    public int Size { get; set; }

    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    
    public DateTime? PhotographedAt { get; set; }
    public string? Camera { get; set; } = "";
    public string? Lens { get; set; } = "";
    public double? Lat { get; set; }
    public double? Lng { get; set; }
    
    public bool IsPrivate { get; set; }
    
    public int Rating { get; set; }
    
    public int? SetId { get; set; }
    [JsonIgnore]
    public PictureSet? Set { get; set; }
    
    public DateTime UpdatedAt { get; set; }
    
    public IList<Tag> Tags { get; set; } = [];
    
    public bool WebsiteSizesExist { get; set; }
    public string? Website1xUrl { get; set; }
    public string? Website2xUrl { get; set; }

    public (int, int) GetDownsizedDimensions(int targetSize)
    {
        // determine target sizes, always keeping aspect ratio, making the short side fit desired size,
        // returning (0, 0) if the image is already too small anyway
        double scale;
        if (Width > Height)
        {
            if (Height < targetSize)
            {
                // too small
                return (0, 0);
            }
            scale = targetSize * 1.0 / Height;
        }
        else
        {
            if (Width < targetSize)
            {
                // too small
                return (0, 0);
            }
            scale = targetSize * 1.0 / Width;
        }
            
        return ((int) (Width * scale), (int) (Height * scale));
    }
}