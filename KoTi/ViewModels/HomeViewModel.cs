namespace KoTi.ViewModels;

public class HomeViewModel
{
    public int TotalPictures { get; set; }
    public required IDictionary<string, int> TotalPosts { get; set; }
    public required IDictionary<string, int> TotalArticles { get; set; }
    public required IDictionary<string, int> TotalBooks { get; set; }
    public required string Version { get; set; }
    public DateTime DatabaseLastPublishedAt { get; set; }
    public long DatabaseSize { get; set; }
    public required string S3Bucket { get; set; }
}