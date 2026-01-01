namespace KoTi.ResponseModels;

public class Stats
{
    public int TotalPictures { get; set; }

    public int TotalPicturesWithNoLocation { get; set; }

    public int TotalPlaces { get; set; }
    
    public int TotalPosts { get; set; }
    
    public int TotalArticles { get; set; }
    
    public DateTime DatabaseLastPublishedAt { get; set; }
    
    public long DatabaseSize { get; set; }

    public string S3Bucket { get; set; } = "";
}