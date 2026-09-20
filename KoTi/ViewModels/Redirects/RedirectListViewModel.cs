using Holvi.Models;

namespace KoTi.ViewModels.Redirects;

public class RedirectListViewModel : PaginatedViewModel
{
    public required IList<RedirectItemViewModel> Redirects { get; set; }
}

public class RedirectItemViewModel
{
    public required Redirect Redirect { get; set; }
    
    // "Post"/"Article" for post:XXX/article:XXX targets, null for plain URLs
    public string? TargetKind { get; set; }
    // null if target post/article does not exist
    public string? TargetTitle { get; set; }
    public string? TargetEditUrl { get; set; }
    public string? TargetHash { get; set; }
}
