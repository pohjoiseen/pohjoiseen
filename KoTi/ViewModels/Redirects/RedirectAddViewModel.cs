namespace KoTi.ViewModels.Redirects;

public class RedirectAddViewModel
{
    // "url", "post" or "article"
    public required string Kind { get; set; }
    public required string Language { get; set; }
}
