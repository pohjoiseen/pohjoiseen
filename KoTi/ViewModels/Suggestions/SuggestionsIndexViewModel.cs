namespace KoTi.ViewModels.Suggestions;

public class SuggestionsIndexViewModel
{
    /// <summary>
    /// A post, article or book with pending suggestions.
    /// </summary>
    public record Target(string Kind, int Id, string Language, string Title, int Count, IList<string> Batches, string EditUrl);

    public required IList<Target> Targets { get; set; }
}
