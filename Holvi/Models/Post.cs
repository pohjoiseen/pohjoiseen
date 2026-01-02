using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Holvi.Models;

public class Post : IContentEntity
{
    /// <summary>
    /// Helper class: Geo point definition to show on a map, one or more can be associated to a post.
    /// </summary>
    public record GeoPoint
    {
        public string? Title { get; init; }
        public string? Subtitle { get; init; }
        public string? Description { get; init; }
        public string? TitleImage { get; init; }
        public string? Anchor { get; init; }
        public double Lat { get; init; }
        public double Lng { get; init; }
        public int? Zoom { get; init; }
        public string? Icon { get; init; }
        public IList<string>? Maps { get; init; }
        public IList<Link>? Links { get; init; }
    }

    /// <summary>
    /// Helper struct: Coat of arms image definition, one or more can be associated to a post.
    /// </summary>
    public record CoatOfArms
    {
        public string Url { get; init; } = "";
        public int? Size { get; init; }
    }

    /// <summary>
    /// Helper class: Link definition for GeoPoint
    /// </summary>
    public record Link
    {
        public string Label { get; init; } = "";
        public string Path { get; init; } = "";
    }
    
    public int Id { get; init; }
    
    public bool Draft { get; set; }

    [StringLength(255)]
    public string Name { get; set; } = "";

    [JsonConverter(typeof(DateOnlyJsonConverter))]
    public DateOnly Date { get; set; }

    public string ContentMD { get; set; } = "";

    [StringLength(2)]
    public string Language { get; set; } = "";
    
    public string Title { get; set; } = "";

    public string Description { get; set; } = "";
    
    public bool Mini { get; set; }

    public int? TitlePictureId { get; set; }
    public Picture? TitlePicture { get; set; }

    /// <summary>
    /// For title images as header background, sets background-position-y
    /// </summary>
    public int? TitleImageOffsetY { get; set; }

    /// <summary>
    /// true = title image (if present) is shown as a regular image in the beginning of the post
    /// false = title image (if present) is shown as a background image in the big header
    /// </summary>
    public bool? TitleImageInText { get; set; }

    /// <summary>
    /// Used when titleImageInText=true
    /// </summary>
    public string? TitleImageCaption { get; set; }

    /// <summary>
    /// Free-form description of the post timing, shown in header ("Pictures from May 2018, text from February 2023")
    /// </summary>
    public string? DateDescription { get; set; }

    /// <summary>
    /// Free-form short description of where approximately the place mentioned is (not precise address or directions)
    /// </summary>
    public string? LocationDescription { get; set; }

    /// <summary>
    /// Address of the place described (a line or a paragraph)
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// Public transport accessibility of the place described (if at all possible)
    /// </summary>
    public string? PublicTransport { get; set; }

    /// <summary>
    /// Link to a Twitter thread
    /// </summary>
    public string? Twitter { get; set; }

    /// <summary>
    /// One of more coats of arms
    /// </summary>
    public IList<CoatOfArms>? CoatsOfArms { get; set; }

    public IList<GeoPoint>? Geo { get; set; }
    
    public DateTime UpdatedAt { get; set; }

    public override string ToString()
    {
        return $"{Date.ToString("yyyy-MM-dd")}-{Name}";
    }
    
    public sealed class DateOnlyJsonConverter : JsonConverter<DateOnly>
    {
        public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return DateOnly.FromDateTime(reader.GetDateTime());
        }

        public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
        {
            var isoDate = value.ToString("O");
            writer.WriteStringValue(isoDate);
        }
    }
}