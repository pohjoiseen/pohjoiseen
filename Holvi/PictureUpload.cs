using System.Globalization;
using Holvi.Models;
using Holvi.ResponseModels;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.Processing;

namespace Holvi;

/// <summary>
/// This is the high-level interface for picture S3 storage.
/// </summary>
public class PictureUpload
{
    private readonly PictureStorage _pictureStorage;
    private readonly HolviDbContext _context;

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

    public PictureUpload(PictureStorage pictureStorage, HolviDbContext context)
    {
        _pictureStorage = pictureStorage;
        _context = context;
    }
    
    /// <summary>
    /// Uploads a picture file to S3, creating also its required resized versions (thumbnail and "details view").
    /// Detects duplicates, will not reupload anything then.
    /// </summary>
    /// <param name="input">File to upload, as a stream</param>
    /// <param name="hash">SHA-1 hash of the content</param>
    /// <param name="filename">Original filename</param>
    /// <returns>Uploaded URLs</returns>
    public async Task<UploadResult> UploadAsync(Stream input, string hash, string filename)
    {
        string baseOutputName = $"{hash}/{filename}";
        
        // first of all check if it already exists in storage 
        string? existingKey = await _pictureStorage.CheckPictureAlreadyUploadedAsync(baseOutputName);
        if (existingKey != null)
        {
            // check if it also exists in database, if so, nothing to do
            var existing = await _context
                .Pictures
                .Where(p => p.Hash == hash).FirstOrDefaultAsync();

            if (existing != null)
            {
                return new UploadResult
                {
                    ExistedInStorage = true,
                    ExistingId = existing.Id,
                    Hash = hash,
                    PictureUrl = existing.Url,
                    ThumbnailUrl = existing.ThumbnailUrl,
                    DetailsUrl = existing.DetailsUrl,
                    Width = existing.Width,
                    Height = existing.Height,
                    Size = existing.Size,
                    PhotographedAt = existing.PhotographedAt,
                    Camera = existing.Camera,
                    Lens = existing.Lens,
                    Lat = existing.Lat,
                    Lng = existing.Lng
                };
            }
        }
        
        var result = new UploadResult
        {
            ExistedInStorage = existingKey is not null,
            Hash = hash,
            PictureUrl = baseOutputName,
            ThumbnailUrl = baseOutputName,
            DetailsUrl = baseOutputName
        };

        input.Seek(0, SeekOrigin.Begin);
        using (var inputImage = await Image.LoadAsync(input))
        {
            int width = inputImage.Width, height = inputImage.Height;
            result.Width = width;
            result.Height = height;
            result.Size = (int)input.Length;

            // generate thumbnail, if not exists yet
            // always match height, resize width as important
            // if height is no bigger than target size, do not do anything else, base filename will be used
            if (height > Picture.ThumbnailSize * 2)
            {
                string? outputName = existingKey is null
                    ? null
                    : await FindDownsizedVersionAsync(baseOutputName, Picture.ThumbnailSuffix);
                if (outputName is null)
                {
                    double scale = Picture.ThumbnailSize * 2.0 / height;
                    outputName = await UploadDownsizedVersionAsync(inputImage, (int)(width * scale),
                        Picture.ThumbnailSize * 2, baseOutputName, Picture.ThumbnailSuffix, false);
                }

                result.ThumbnailUrl = outputName;
            }
            
            // generate "details" size, also if not exists
            // same but match width
            if (width > Picture.DetailsSize * 2)
            {
                string? outputName = existingKey is null
                    ? null
                    : await FindDownsizedVersionAsync(baseOutputName, Picture.DetailsSuffix);
                if (outputName is null)
                {
                    double scale = Picture.DetailsSize * 2.0 / width;
                    outputName = await UploadDownsizedVersionAsync(inputImage, Picture.DetailsSize * 2,
                        (int)(height * scale), baseOutputName, Picture.DetailsSuffix, false);
                }

                result.DetailsUrl = outputName;
            }
            
            // extract EXIF metadata
        
            // find original datetime from EXIF, if any, default to now
            DateTime date = DateTime.UtcNow;
            var originalDateTimeString = (string?)inputImage.Metadata.ExifProfile?.Values
                .FirstOrDefault(v => v.Tag == ExifTag.DateTimeOriginal)?.GetValue();
            if (originalDateTimeString != null && originalDateTimeString != "0000:00:00 00:00:00")
            {
                DateTime.TryParseExact(originalDateTimeString, "yyyy:MM:dd HH:mm:ss",
                    CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out date);
                date = date.ToUniversalTime();
            }
            result.PhotographedAt = date;

            // find coordinates from EXIF, if any
            var exif = inputImage.Metadata.ExifProfile;
            result.Lat = GetExifCoordinate(exif, ExifTag.GPSLatitude, ExifTag.GPSLatitudeRef, "S");
            result.Lng = GetExifCoordinate(exif, ExifTag.GPSLongitude, ExifTag.GPSLongitudeRef, "W");
            
            // camera and lens
            if (inputImage.Metadata.ExifProfile?.TryGetValue(ExifTag.Model, out IExifValue<string>? model) == true)
            { 
                result.Camera = model.Value;
            }
            if (inputImage.Metadata.ExifProfile?.TryGetValue(ExifTag.LensModel, out IExifValue<string>? lensModel) == true)
            {
                if (!String.IsNullOrWhiteSpace(result.Camera))
                {
                    result.Lens = lensModel.Value.TrimStart(result.Camera + "_").ToString();
                }
                else
                {
                    result.Lens = lensModel.Value;
                }
            }
        }
        
        // upload the original image last, unless already done
        if (existingKey is null)
        {
            input.Seek(0, SeekOrigin.Begin);
            await _pictureStorage.UploadPictureAsync(baseOutputName, input);
        }

        // add public prefixes to all URLs
        result.DetailsUrl = _pictureStorage.PublicUrl + result.DetailsUrl;
        result.ThumbnailUrl = _pictureStorage.PublicUrl + result.ThumbnailUrl;
        result.PictureUrl = _pictureStorage.PublicUrl + result.PictureUrl;
        
        return result;
    }

    /// <summary>
    /// Creates website versions of a picture (1x, 2x) and uploads them to S3, saves URLs
    /// to Picture entity.  These versions are optional.  Will detect if these versions already exist
    /// but URLs were not saved for some reason.  Will not do anything if Picture already has
    /// flag WebsiteSizesExist set to true. 
    /// </summary>
    /// <param name="picture">Picture to process</param>
    /// <returns>true if needed to do anything</returns>
    /// <exception cref="Exception"></exception>
    public async Task<bool> EnsureWebsiteVersionsExist(Picture picture)
    {
        if (picture.WebsiteSizesExist)
        {
            return false;
        }

        Image? image = null;

        var sizes = new Dictionary<string, int>
        {
            { ".1x", Picture.WebsiteSize },
            { ".2x", Picture.WebsiteSize * 2 }
        };
        
        foreach (var size in sizes)
        {
            string sizeSuffix = size.Key;
            int targetSize = size.Value;

            // determine target sizes
            (int width, int height) = picture.GetDownsizedDimensions(targetSize);
            if (width == 0 || height == 0)
            {
                continue;  // too small, skip this size
            }

            // might be already resized but not in database
            var baseName = $"{picture.Hash}/{picture.Filename}";
            var resizedName = await FindDownsizedVersionAsync(baseName, sizeSuffix);
            if (resizedName == null)
            {
                // load image if not done yet
                if (image == null)
                {
                    await using var responseStream = await _pictureStorage.DownloadPictureAsync(picture.Url);
                    image = await Image.LoadAsync(responseStream);
                }

                // actually resize and save, clearing metadata
                resizedName = await UploadDownsizedVersionAsync(image, width, height, baseName, sizeSuffix, true);
            }

            if (sizeSuffix == ".1x")
            {
                picture.Website1xUrl = _pictureStorage.PublicUrl + resizedName;
            }

            if (sizeSuffix == ".2x")
            {
                picture.Website2xUrl = _pictureStorage.PublicUrl + resizedName;
            }
        }

        picture.WebsiteSizesExist = true;
        await _context.SaveChangesAsync();
        
        if (image != null)
        {
            image.Dispose();
        }
        return image != null;
    }
    
    /// <summary>
    /// Read GPS latitude or longitude from EXIF (degrees, minutes, seconds as rationals, plus N/S or E/W reference).
    /// </summary>
    private static double? GetExifCoordinate(ExifProfile? exif, ExifTag<Rational[]> valueTag, ExifTag<string> refTag,
        string negativeRef)
    {
        if (exif?.TryGetValue(valueTag, out var parts) != true || parts!.Value?.Length != 3)
        {
            return null;
        }

        var degrees = parts.Value[0].ToDouble();
        var minutes = parts.Value[1].ToDouble();
        var seconds = parts.Value[2].ToDouble();
        if (!double.IsFinite(degrees) || !double.IsFinite(minutes) || !double.IsFinite(seconds))
        {
            return null;
        }

        var coordinate = degrees + minutes / 60D + seconds / 3600D;
        if (exif.TryGetValue(refTag, out var reference) &&
            reference!.Value?.Trim().Equals(negativeRef, StringComparison.OrdinalIgnoreCase) == true)
        {
            coordinate = -coordinate;
        }
        return coordinate;
    }

    /// <summary>
    /// Find a resized version (by suffix) of an already uploaded picture, in whatever format it was saved.
    /// </summary>
    /// <returns>Key in storage, or null if not found</returns>
    private Task<string?> FindDownsizedVersionAsync(string baseName, string suffix)
    {
        // storage lookup is by prefix, so this finds e.g. both older .t.jpg and newer .t.webp
        return _pictureStorage.CheckPictureAlreadyUploadedAsync(GetFilenameWithSuffix(baseName, suffix, "."));
    }

    /// <summary>
    /// Resize a picture and upload it.  Saves as WebP, lossy for lossy originals (JPEG, lossy WebP),
    /// lossless otherwise (PNG, lossless WebP); unless too large for WebP, then JPEG/PNG respectively.
    /// </summary>
    /// <returns>Key in storage</returns>
    private async Task<string> UploadDownsizedVersionAsync(Image image, int width, int height, string baseName,
        string suffix, bool clearMetadata)
    {
        var format = image.Metadata.DecodedImageFormat;
        bool isLossy = format is JpegFormat ||
                       (format is WebpFormat && image.Metadata.GetWebpMetadata().FileFormat == WebpFileFormatType.Lossy);

        IImageEncoder encoder;
        string extension;
        if (width <= WebpMaxDimension && height <= WebpMaxDimension)
        {
            encoder = isLossy
                ? new WebpEncoder { FileFormat = WebpFileFormatType.Lossy, Quality = WebpQualityLevel }
                : new WebpEncoder { FileFormat = WebpFileFormatType.Lossless };
            extension = ".webp";
        }
        else if (isLossy)
        {
            encoder = new JpegEncoder { Quality = JpegQualityLevel };
            extension = ".jpg";
        }
        else
        {
            encoder = new PngEncoder();
            extension = ".png";
        }

        using var resizedImage = image.Clone(x => x.Resize(width, height));
        if (clearMetadata)
        {
            resizedImage.Metadata.ExifProfile = null;
            resizedImage.Metadata.XmpProfile = null;
        }

        var name = GetFilenameWithSuffix(baseName, suffix, extension);
        var output = new MemoryStream();
        await resizedImage.SaveAsync(output, encoder);
        output.Seek(0, SeekOrigin.Begin);
        await _pictureStorage.UploadPictureAsync(name, output);
        return name;
    }

    private static string GetFilenameWithSuffix(string filename, string suffix, string newExtension)
    {
        string extension = Path.GetExtension(filename);
        return filename.Substring(0, filename.Length - extension.Length) + suffix + newExtension;
    }
}
