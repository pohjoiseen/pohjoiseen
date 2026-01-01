namespace KoTi.ViewModels;

public abstract class PaginatedViewModel
{
    public required int Limit { get; set; }
    public required int Offset { get; set; }
    public required int Total { get; set; }
}