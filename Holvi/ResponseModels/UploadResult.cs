namespace Holvi.ResponseModels;

public class UploadResult
{
    public string Hash { get; set; } = "";
    public string PictureUrl { get; set; } = "";

    public string ThumbnailUrl { get; set; } = "";

    public string DetailsUrl { get; set; } = "";
    
    public int Width { get; set; }
    public int Height { get; set; }
    public int Size { get; set; }

    public DateTime? PhotographedAt { get; set; }
    public string? Camera { get; set; } = "";
    public string? Lens { get; set; } = "";
    public double? Lat { get; set; }
    public double? Lng { get; set; }
    
    public bool ExistedInStorage { get; set; }
    public int? ExistingId { get; set; }
}