using Holvi.Models;

namespace KoTi.ViewModels.Pictures;

public class PictureListViewModel : PaginatedViewModel
{
    public required string ComponentId { get; set; }
    public required IList<Picture> Pictures { get; set; }
    public PictureSet? PictureSet { get; set; }
    public string? PictureSetSearchQuery { get; set; }
    public required IDictionary<int, IEnumerable<string>> ChildrenPictureSetThumbnails { get; set; }
}

