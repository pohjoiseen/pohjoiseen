using KoTi.Models;

namespace KoTi.ResponseModels;

public class PictureSetResponseDTO
{
    public int Id { get; set; }

    public string Name { get; set; } = "";
    
    public bool IsPrivate { get; set; }

    public IEnumerable<PictureSetResponseDTO> Children { get; set; } = new List<PictureSetResponseDTO>();

    public IEnumerable<string> ThumbnailUrls { get; set; } = new List<string>();

    public static PictureSetResponseDTO FromModel(PictureSet set,
        IEnumerable<string> thumbnailUrls,
        IEnumerable<PictureSetResponseDTO>? children = null)
    {
        return new PictureSetResponseDTO
        {
            Id = set.Id,
            Name = set.Name,
            IsPrivate = set.IsPrivate,
            Children = children ?? new List<PictureSetResponseDTO>(),
            ThumbnailUrls = thumbnailUrls
        };
    }
}