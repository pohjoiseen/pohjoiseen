using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Holvi.Models;

namespace KoTi.RequestModels;

public class PostRequestDTO
{
    [Required] public string Name { get; set; } = "";

    [Required]
    [JsonConverter(typeof(Post.DateOnlyJsonConverter))]
    public DateOnly Date { get; set; }

    public bool Draft { get; set; }
    
    public string ContentMD { get; set; } = "";

    [Required] public string Title { get; set; } = "";

    public string Description { get; set; } = "";
    
    public bool Mini { get; set; }

    public int? TitlePictureId { get; set; }

    public bool? TitleImageInText { get; set; }

    public string? TitleImageCaption { get; set; }

    public string? DateDescription { get; set; }

    public string? LocationDescription { get; set; }

    public string? Address { get; set; }

    public string? PublicTransport { get; set; }

    public IList<Post.CoatOfArms>? CoatsOfArms { get; set; }

    public IList<Post.GeoPoint>? Geo { get; set; }

    public void ToModel(Post model)
    {
        model.Name = Name;
        model.Date = Date;
        model.ContentMD = ContentMD;
        model.Title = Title;
        model.Description = Description;
        model.Mini = Mini;
        model.Draft = Draft;
        model.TitlePictureId = TitlePictureId;
        model.TitleImageInText = TitleImageInText;
        model.TitleImageCaption = TitleImageCaption;
        model.DateDescription = DateDescription;
        model.LocationDescription = LocationDescription;
        model.Address = Address;
        model.PublicTransport = PublicTransport;
        model.CoatsOfArms = CoatsOfArms;
        model.Geo = Geo;
    }
}