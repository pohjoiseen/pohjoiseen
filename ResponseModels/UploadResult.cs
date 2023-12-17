namespace KoTi.ResponseModels;

public class UploadResult
{
    public string PictureUrl { get; set; } = "";

    public string ThumbnailUrl { get; set; } = "";

    public string DetailsUrl { get; set; } = "";

    public int? ExistingId = null;
}