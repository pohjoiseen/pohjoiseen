namespace KoTi.LiveJournal;

public enum LJGalleryMode
{
    /// <summary>Gallery pictures become ordinary pictures in a row</summary>
    Individual,
    /// <summary>&lt;lj-gallery&gt;</summary>
    LJGallery,
    /// <summary>&lt;lj-gallery&gt; if more than 3 pictures, otherwise individual pictures</summary>
    LJGalleryIfMoreThan3
}

public enum LJAsideMode
{
    Remove,
    Blockquote
}

public enum LJImageSourceMode
{
    /// <summary>src (1x) and srcset (1x, 2x)</summary>
    SrcAndSrcset,
    /// <summary>srcset only, no src</summary>
    SrcsetOnly,
    /// <summary>src set to 2x version, no srcset</summary>
    Src2x
}

/// <summary>
/// Settings for LiveJournal crosspost HTML (<see cref="LJHtmlConverter"/>).  Defaults are what is normally wanted.
/// </summary>
public class LJCrosspostOptions
{
    public bool TitlePicture { get; set; } = true;
    public LJGalleryMode Galleries { get; set; } = LJGalleryMode.Individual;
    public LJAsideMode Asides { get; set; } = LJAsideMode.Remove;
    public bool RemoveAlts { get; set; }
    public bool RemoveLazyLoading { get; set; }
    public LJImageSourceMode ImageSource { get; set; } = LJImageSourceMode.SrcsetOnly;
    public bool LinkPicturesToOriginals { get; set; } = true;
    public bool HeadingIds { get; set; } = true;
    public bool LJCut { get; set; } = true;
    public bool LinkToOriginalPost { get; set; } = true;
    public bool RemoveNewlines { get; set; }
}
