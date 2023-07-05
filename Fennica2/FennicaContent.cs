using Teos;

namespace Fennica2;

/// <summary>
/// Base class for content in Fennica project.  The property that all Fennica content always has is language.
/// </summary>
public abstract class FennicaContent : Content
{
    /// <summary>
    /// Language of the content item, e. g. "en", "fi".
    /// </summary>
    public string Language { get; set; } = "";

    public virtual FennicaContent Clone()
    {
        return (FennicaContent) MemberwiseClone();
    }
}

