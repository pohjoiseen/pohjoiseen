namespace KoTi.ViewModels;

public abstract class AbstractContentViewModel
{
    public int Id { get; init;  }

    public abstract string KindLink { get; }
    public abstract string KindPlural { get; }
    public abstract string FennicaURL { get; }
    
    public string Title { get; set; } = "";
    public string? ContentMD { get; set; } = "";
    public string Language { get; set; } = "ru";
    public IDictionary<string, int> AllLanguages { get; set; } = new Dictionary<string, int>();
    public bool Draft { get; set; }

    /// <summary>
    /// Version of the entity this model was loaded from, saving fails if the entity has a different one by then.
    /// </summary>
    public int Version { get; set; }
}