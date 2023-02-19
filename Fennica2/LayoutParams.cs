namespace Fennica2;

/// <summary>
/// Parameters for _FennicaLayout.cshtml.  Mostly self-explanatory.
/// </summary>
public class LayoutParams
{
    public string Title { get; set; } = "";

    public string Description { get; set; } = "";

    public DateOnly? PublishDate { get; set; }

    /// <summary>
    /// Generate appropriate og meta tags?
    /// </summary>
    public bool IsOpenGraphArticle { get; set; }

    public string TitleImage { get; set; } = "";

    public string Language { get; set; } = "";

    public IDictionary<string, string> LanguageVersions { get; set; } = new Dictionary<string, string>();
    
    public string PrevPath { get; set; } = "";

    public string NextPath { get; set; } = "";

    public string PrevTitle { get; set; } = "";

    public string NextTitle { get; set; } = "";

    public bool NoIndex { get; set; }

    public string RSSLink { get; set; } = "";

    public string BodyClass { get; set; } = "";

    /// <summary>
    /// Set = display bigger header, value = highlighted page in the header
    /// Not set = display small header with just links between language versions of the current page
    /// </summary>
    public string BigHeaderPage { get; set; } = "";
}