using Holvi.Models;

namespace KoTi.ViewModels.Pictures;

public class PictureListViewModel : PaginatedViewModel
{
    public required string ComponentId { get; set; }
    public required IList<Picture> Pictures { get; set; }
    public PictureSet? PictureSet { get; set; }
    public string? PictureSetSearchQuery { get; set; }
    public required IDictionary<int, IEnumerable<string>> ChildrenPictureSetThumbnails { get; set; }

    // When true, folder/search/paging navigation targets the standalone /pictures/folders
    // and /pictures/all page (with hx-push-url) instead of the embedded List endpoint used
    // from the content editor's picture picker dialog.
    public bool UseLinks { get; set; }
}

