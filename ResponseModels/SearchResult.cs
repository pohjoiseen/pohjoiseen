namespace KoTi.ResponseModels;

public class SearchResult
{
    public string TableName { get; set; }
    public int TableId { get; set; }
    public string Title { get; set; }
    public string Text { get; set; }
    public double Rank { get; set; }
}