using System.Buffers.Binary;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using NetVips;

namespace Holvi;

/// <summary>
/// A picture file in memory: dimensions, EXIF data and resizing.  Decoding, resizing and encoding is done with libvips
/// (NetVips), EXIF is read with MetadataExtractor.
/// </summary>
public class PictureImage
{
    /// <summary>
    /// This is only for resized versions, the original is always kept as is.  75 seems to be a good compromise
    /// </summary>
    private const int JpegQualityLevel = 75;

    /// <summary>
    /// Same for lossy WebP, which resized versions are normally saved as since 3.5 (before, they were .jpg/.png;
    /// these are not reconverted)
    /// </summary>
    private const int WebpQualityLevel = 75;

    /// <summary>
    /// WebP cannot store larger images, resized versions of very long panoramas fall back to JPEG/PNG
    /// </summary>
    private const int WebpMaxDimension = 16383;

    private readonly byte[] _content;

    /// <summary>Width in pixels as displayed, that is, with EXIF orientation applied</summary>
    public int Width { get; }

    /// <summary>Height in pixels as displayed, that is, with EXIF orientation applied</summary>
    public int Height { get; }

    /// <summary>JPEG or lossy WebP, as opposed to PNG, lossless WebP etc.</summary>
    public bool IsLossy { get; }

    static PictureImage()
    {
        // only the loaders libvips considers safe for untrusted input (the usual image formats)
        global::NetVips.NetVips.BlockUntrusted = true;
        // no point in caching operations, every picture is processed once; saves memory
        Cache.Max = 0;
    }

    /// <exception cref="VipsException">Not an image in a supported format</exception>
    public PictureImage(byte[] content)
    {
        _content = content;
        // this reads only the header
        using var header = Image.NewFromBuffer(content);
        // EXIF orientations 5 to 8 are rotated by 90 degrees
        bool isRotated = header.Contains("orientation") && (int)header.Get("orientation") is >= 5 and <= 8;
        Width = isRotated ? header.Height : header.Width;
        Height = isRotated ? header.Width : header.Height;
        var loader = (string)header.Get("vips-loader");
        IsLossy = loader.StartsWith("jpegload") || (loader.StartsWith("webpload") && IsLossyWebp(content));
    }

    /// <summary>
    /// Resize to exactly the given size, turning the picture upright if it has EXIF orientation.  Encodes as WebP, lossy for lossy originals (JPEG, lossy WebP),
    /// lossless otherwise (PNG, lossless WebP); unless too large for WebP, then JPEG/PNG respectively.
    /// Color profile is always kept, EXIF and XMP unless <paramref name="clearMetadata"/>.
    /// </summary>
    /// <returns>Encoded picture and the filename extension for its format (with dot)</returns>
    public (byte[] Content, string Extension) Resize(int width, int height, bool clearMetadata)
    {
        // decodes anew every time, but (for JPEG and WebP) directly at a reduced size, which is much faster
        // and needs much less memory than decoding the entire original once
        using var thumbnail = Image.ThumbnailBuffer(_content, width, height: height, size: Enums.Size.Force);
        // CMYK with a profile was already converted to sRGB by thumbnail, this is for CMYK without one
        using var resized = thumbnail.Interpretation == Enums.Interpretation.Cmyk
            ? thumbnail.Colourspace(Enums.Interpretation.Srgb)
            : thumbnail.Copy();

        var keep = clearMetadata
            ? Enums.ForeignKeep.Icc
            : Enums.ForeignKeep.Exif | Enums.ForeignKeep.Xmp | Enums.ForeignKeep.Icc;
        if (width <= WebpMaxDimension && height <= WebpMaxDimension)
        {
            return (resized.WebpsaveBuffer(q: WebpQualityLevel, lossless: !IsLossy, keep: keep), ".webp");
        }
        if (IsLossy)
        {
            return (resized.JpegsaveBuffer(q: JpegQualityLevel, keep: keep), ".jpg");
        }
        return (resized.PngsaveBuffer(keep: keep), ".png");
    }

    /// <summary>
    /// EXIF data we are interested in, all null if there is no EXIF data at all.
    /// </summary>
    public PictureExif ReadExif()
    {
        IReadOnlyList<MetadataExtractor.Directory> directories;
        try
        {
            directories = ImageMetadataReader.ReadMetadata(new MemoryStream(_content));
        }
        catch (Exception e) when (e is ImageProcessingException or IOException)
        {
            return new PictureExif();
        }

        string? GetString<T>(int tag) where T : MetadataExtractor.Directory =>
            directories.OfType<T>().FirstOrDefault(d => d.ContainsTag(tag))?.GetString(tag);

        double? GetCoordinate(int valueTag, int refTag, string negativeRef)
        {
            // degrees, minutes, seconds as rationals, plus N/S or E/W reference
            var parts = directories.OfType<GpsDirectory>().FirstOrDefault(d => d.ContainsTag(valueTag))
                ?.GetRationalArray(valueTag);
            if (parts?.Length != 3 || parts.Any(p => p.Denominator == 0))
            {
                return null;
            }

            var coordinate = parts[0].ToDouble() + parts[1].ToDouble() / 60D + parts[2].ToDouble() / 3600D;
            if (GetString<GpsDirectory>(refTag)?.Trim().Equals(negativeRef, StringComparison.OrdinalIgnoreCase) == true)
            {
                coordinate = -coordinate;
            }
            return coordinate;
        }

        return new PictureExif
        {
            DateTimeOriginal = GetString<ExifSubIfdDirectory>(ExifDirectoryBase.TagDateTimeOriginal),
            Camera = GetString<ExifIfd0Directory>(ExifDirectoryBase.TagModel),
            Lens = GetString<ExifSubIfdDirectory>(ExifDirectoryBase.TagLensModel),
            Lat = GetCoordinate(GpsDirectory.TagLatitude, GpsDirectory.TagLatitudeRef, "S"),
            Lng = GetCoordinate(GpsDirectory.TagLongitude, GpsDirectory.TagLongitudeRef, "W")
        };
    }

    /// <summary>
    /// Whether a WebP file is lossy (VP8) rather than lossless (VP8L); libvips does not tell.
    /// </summary>
    public static bool IsLossyWebp(ReadOnlySpan<byte> content)
    {
        if (content.Length < 12 || !content[..4].SequenceEqual("RIFF"u8) || !content[8..12].SequenceEqual("WEBP"u8))
        {
            return false;
        }

        // chunks: four-character code, little-endian size, data padded to even size
        int position = 12;
        while (position + 8 <= content.Length)
        {
            var chunk = content.Slice(position, 4);
            if (chunk.SequenceEqual("VP8 "u8))
            {
                return true;
            }
            if (chunk.SequenceEqual("VP8L"u8))
            {
                return false;
            }
            long size = BinaryPrimitives.ReadUInt32LittleEndian(content.Slice(position + 4, 4));
            long next = position + 8 + size + (size & 1);
            if (next > content.Length)
            {
                break;
            }
            position = (int)next;
        }
        return false;
    }
}

public record PictureExif
{
    /// <summary>As in EXIF, "yyyy:MM:dd HH:mm:ss" in local time of the camera</summary>
    public string? DateTimeOriginal { get; init; }
    public string? Camera { get; init; }
    public string? Lens { get; init; }
    public double? Lat { get; init; }
    public double? Lng { get; init; }
}
