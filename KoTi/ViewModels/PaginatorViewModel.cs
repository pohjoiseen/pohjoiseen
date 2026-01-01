namespace KoTi.ViewModels;

public class PaginatorViewModel
{
    public required PaginatedViewModel Data { get; set; }
    public required string HxTarget { get; set; }
    public required string BaseUrl { get; set; }
}