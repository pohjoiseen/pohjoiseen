namespace KoTi.ViewModels;

public class SearchResultsViewModel : PaginatedViewModel
{
    public required string Query { get; set; }
    
    public required IList<SearchResultViewModel> Results { get; set; }
    
    public string? Error { get; set; }

    public class SearchResultViewModel
    {
        public string TableName { get; set; } = "";
        public int TableId { get; set; }
        public string Title { get; set; } = "";
        public string Text { get; set; } = "";
        public double Rank { get; set; }
    }
}