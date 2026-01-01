namespace KoTi.ViewModels;

public abstract class AbstractContentViewModel
{
    public int Id { get; init;  }

    public abstract string KindPlural { get; }
    public abstract string FennicaURL { get; }
    
    public string Title { get; set; } = "";
    public string ContentMD { get; set; } = "";
    public string Language { get; set; } = "ru";
    public IList<string> AllLanguages { get; set; } = [];
    public bool Draft { get; set; }
}