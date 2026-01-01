using System.Globalization;
using Holvi.Models;
using Holvi.ResponseModels;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
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
                    PictureUrl = _pictureStorage.PublicUrl + existingKey,
                    ThumbnailUrl = _pictureStorage.PublicUrl +
                                   GetFilenameWithSuffix(existingKey, Picture.ThumbnailSuffix, true),
                    DetailsUrl = _pictureStorage.PublicUrl +
                                 GetFilenameWithSuffix(existingKey, Picture.DetailsSuffix, true),
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
                string outputName = GetFilenameWithSuffix(baseOutputName, Picture.ThumbnailSuffix, true);
                if (existingKey is null || await _pictureStorage.CheckPictureAlreadyUploadedAsync(outputName) is null)
                {
                    double scale = Picture.ThumbnailSize * 2.0 / height;
                    MemoryStream output = new MemoryStream();
                    using (var resizedImage =
                           inputImage.Clone(x => x.Resize((int)(width * scale), Picture.ThumbnailSize * 2)))
                    {
                        await resizedImage.SaveAsync(output, new JpegEncoder());
                    }

                    output.Seek(0, SeekOrigin.Begin);
                    await _pictureStorage.UploadPictureAsync(outputName, output);
                }

                result.ThumbnailUrl = outputName;
            }
            
            // generate "details" size, also if not exists
            // same but match width
            if (width > Picture.DetailsSize * 2)
            {
                string outputName = GetFilenameWithSuffix(baseOutputName, Picture.DetailsSuffix, true);
                if (existingKey is null || await _pictureStorage.CheckPictureAlreadyUploadedAsync(outputName) is null)
                {
                    double scale = Picture.DetailsSize * 2.0 / width;
                    MemoryStream output = new MemoryStream();
                    using (var resizedImage =
                           inputImage.Clone(x => x.Resize(Picture.DetailsSize * 2, (int)(height * scale))))
                    {
                        await resizedImage.SaveAsync(output, new JpegEncoder { Quality = JpegQualityLevel });
                    }

                    output.Seek(0, SeekOrigin.Begin);
                    await _pictureStorage.UploadPictureAsync(outputName, output);
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
            if (inputImage.Metadata.ExifProfile?.TryGetValue(ExifTag.GPSLatitude,
                    out IExifValue<Rational[]>? latitudeParts) == true && latitudeParts.Value?.Length == 3)
            {
                uint degrees = latitudeParts.Value[0].Numerator;
                double minutes = latitudeParts.Value[1].Numerator / 60D;
                double seconds = (latitudeParts.Value[2].Numerator / (double)latitudeParts.Value[2].Denominator) /
                                 3600D;
                result.Lat = degrees + minutes + seconds;
            }
            if (inputImage.Metadata.ExifProfile?.TryGetValue(ExifTag.GPSLongitude,
                    out IExifValue<Rational[]>? longitudeParts) == true && longitudeParts.Value?.Length == 3)
            {
                uint degrees = longitudeParts.Value[0].Numerator;
                double minutes = longitudeParts.Value[1].Numerator / 60D;
                double seconds = (longitudeParts.Value[2].Numerator / (double)longitudeParts.Value[2].Denominator) /
                                 3600D;
                result.Lng = degrees + minutes + seconds;
            }
            
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
            var resizedName = $"{picture.Hash}/{GetFilenameWithSuffix(picture.Filename, sizeSuffix, false)}";
	        var alreadyUploaded = await _pictureStorage.CheckPictureAlreadyUploadedAsync(resizedName);
	        if (alreadyUploaded == null)
            {
                // load image if not done yet
                if (image == null)
                {
                    var url = new Uri(picture.Url);
                    using var httpClient = new HttpClient();
                    await using var responseStream = await httpClient.GetStreamAsync(url);
                    image = await Image.LoadAsync(responseStream);
                }

                // actually resize and save
                using Image resizedImage = image.Clone(x => x.Resize(width, height));
                // clear metadata from resized versions
                resizedImage.Metadata.ExifProfile = null;
                resizedImage.Metadata.XmpProfile = null;

                MemoryStream output = new MemoryStream();
                if (picture.Filename.EndsWith(".jpg") || picture.Filename.EndsWith(".jpeg"))
                {
                    await resizedImage.SaveAsync(output, new JpegEncoder { Quality = JpegQualityLevel });
                }
                else if (picture.Filename.EndsWith(".png"))
                {
                    await resizedImage.SaveAsync(output, new PngEncoder());
                }
                else
                {
                    throw new Exception("Could not determine format for " + picture.Filename);
                }
                     
                output.Seek(0,  SeekOrigin.Begin);
                await _pictureStorage.UploadPictureAsync(resizedName, output);
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
    
    public static string GetFilenameWithSuffix(string filename, string suffix, bool forceJpg)
    {
        string extension = Path.GetExtension(filename);
        return filename.Substring(0, filename.Length - extension.Length) + suffix +
               (forceJpg ? ".jpg" : extension);
    }
}
