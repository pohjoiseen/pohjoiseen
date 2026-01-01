using Holvi.Models;

namespace KoTi.ViewModels;

public class FullscreenViewModel : PaginatedViewModel
{
    public required Picture Picture { get; set; }
    public int PreviousId { get; set; }
    public int NextId { get; set; }
    public required IList<string> PreloadPictureUrls { get; set; }
    public int? SetId { get; set; }
    public string? SetSearch { get; set; }
    public string? ChangePage { get; set; }
    public string? OverrideIds { get; set; }
}