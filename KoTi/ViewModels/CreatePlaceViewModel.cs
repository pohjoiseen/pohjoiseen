namespace KoTi.ViewModels;

public class CreatePlaceViewModel
{
    public required int ParentId { get; set; }
    public required string Language { get; set; }
    public required string Name { get; set; }
    public required string Title { get; set; }
    public required double Lat { get; set; }
    public required double Lng { get; set; }
    public bool IsLeaf { get; set; }
}