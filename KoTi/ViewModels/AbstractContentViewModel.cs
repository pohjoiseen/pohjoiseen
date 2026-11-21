using Holvi.Models;

namespace KoTi.ViewModels;

public abstract class AbstractContentViewModel
{
    public int Id { get; init;  }

    public abstract string KindLink { get; }
    public abstract string KindPlural { get; }
    public abstract string FennicaURL { get; }
    
    public string Title { get; set; } = "";
    public string? ContentMD { get; set; } = "";
    public string Language { get; set; } = Fennica3.Fennica3.Languages[0];
    public IDictionary<string, int> AllLanguages { get; set; } = new Dictionary<string, int>();
    public bool Draft { get; set; }

    /// <summary>
    /// Neighbors in the same language, in the order of the list page (for posts previous is the older one).
    /// Only set when loading for the edit page.
    /// </summary>
    public Neighbor? Previous { get; set; }
    public Neighbor? Next { get; set; }

    public record Neighbor(int Id, string Title);

    /// <summary>
    /// Version of the entity this model was loaded from, saving fails if the entity has a different one by then.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Pending suggested edits to the content.  Only set when loading for the edit page.
    /// </summary>
    public IList<Suggestion> Suggestions { get; set; } = [];

    /// <summary>
    /// Earlier texts, without the texts themselves, oldest first.  Only set when loading for the edit page.
    /// </summary>
    public IList<Revision> Revisions { get; set; } = [];

    /// <summary>
    /// Next one of the same kind with pending suggestions, if any.  Only set when loading for the edit page.
    /// </summary>
    public int? NextWithSuggestionsId { get; set; }

    /// <summary>
    /// Submitted with the form: suggestions which were applied to the content in the editor, they become
    /// accepted if the content is saved.
    /// </summary>
    public IList<int> AcceptedSuggestionIds { get; set; } = [];
}