using System.ComponentModel.DataAnnotations;
using KoTi.Models;

namespace KoTi.RequestModels;

public class PictureRequestDTO
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
    [Required] public bool IsPrivate { get; set; }
    [Required] public int Rating { get; set; }
    public int? SetId { get; set; }
    public IList<TagDTO> Tags { get; set; } = [];

    public void ToModel(Picture picture, KoTiDbContext dbContext)
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
        picture.IsPrivate = IsPrivate;
        picture.Rating = Rating;
        picture.SetId = SetId;
        
        foreach (var tagDto in Tags)
        {
            if (picture.Tags.SingleOrDefault(t => t.Id == tagDto.Id) == null)
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

                picture.Tags.Add(newTag);
            } 
        }

        picture.Tags = picture.Tags.Where(tagExisting =>
            tagExisting.Id == 0 || Tags.SingleOrDefault(t => t.Id == tagExisting.Id) != null).ToList();
    }
}