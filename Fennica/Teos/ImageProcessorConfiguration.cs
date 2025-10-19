namespace Teos;

/// <summary>
/// Configuration for ImageProcessor.
/// </summary>
public class ImageProcessorConfiguration
{
    /// <summary>
    /// The input subdirectory relative path for images.
    /// </summary>
    public string InputDir { get; init; } = "";

    /// <summary>
    /// The output subdirectory relative path for images.  Default to same as input.
    /// </summary>
    public string OutputDir { get; init; }

    /// <summary>
    /// Optional list of regexps by which individual files may be excluded.
    /// The whole relative path is matched, not just filename.
    /// </summary>
    public string[] ExcludeRegexps { get; init; }

    /// <summary>
    /// Images extensions to process.  Must be types familiar to ImageSharp library.
    /// </summary>
    public string[] Types { get; init; } = { ".jpg", ".jpeg", ".png", ".gif" };

    /// <summary>
    /// Optional sizes to downscale images to, maps suffix -> short side size.
    /// For e. g. suffix "thumb", a downscaled image for "image.png" would be saved as "image.thumb.png".
    /// Images are only ever downscaled, only every preserving aspect ration and only ever according to
    /// SHORTER side constraint (e. g. 500x1000 image with size=100 would be resized to 100x200).
    /// If the original image is already smaller or equal than the resized version would have been,
    /// the resized version is not generated at all.
    /// All these behaviors are currently hardcoded.
    /// </summary>
    public IDictionary<string, int> Sizes { get; init; }

    /// <summary>
    /// Do not copy over the original image, only resized versions.  By default it is copied (as is).
    /// </summary>
    public bool NoOriginal { get; init; } = false;

    /// <summary>
    /// Quality for JPEG downsizing, not used for other formats.
    /// </summary>
    public int Quality { get; init; } = 75;
}


