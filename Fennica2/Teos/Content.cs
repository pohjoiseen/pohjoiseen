namespace Teos;

/// <summary>
/// Base class for any loaded content item, which corresponds to a file loaded from content directory. 
/// </summary>
public abstract class Content
{
    /// <summary>
    /// Content item name, which must equal the relative path to the item from the content dir. 
    /// </summary>
    public virtual string Name { get; set; } = "";

    /// <summary>
    /// Loaded HTML content.  Content might have more properties which have HTML, or some content types
    /// might not have HTML at all, but the expectation is generally that most content types will have a HTML body.
    /// </summary>
    public virtual string HTML { get; set; } = "";

    /// <summary>
    /// Is this item a draft?  Drafts are not loaded when the corresponding option in TeosEngine is not set.
    /// </summary>
    public bool Draft { get; set; }

    /// <summary>
    /// Returns canonical public URL (relative to wwwroot) for the content item.  For convenience, not used by the engine. 
    /// </summary>
    public abstract string CanonicalURL { get; }

    public override string ToString()
    {
        return $"{Name} ({base.ToString()})";
    }
}