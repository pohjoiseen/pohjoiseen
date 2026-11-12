using System.Globalization;
using Holvi.Models;
using Holvi.ResponseModels;
using Microsoft.EntityFrameworkCore;

namespace Holvi;

/// <summary>
/// This is the high-level interface for picture S3 storage.
/// </summary>
public class PictureUpload
{
    private readonly PictureStorage _pictureStorage;
    private readonly HolviDbContext _context;

    public PictureUpload(PictureStorage pictureStorage, HolviDbContext context)
    {
        _pictureStorage = pictureStorage;
        _context = context;
    }
    
    /// <summary>
    /// Uploads a picture file to S3, creating also its required resized versions (thumbnail and "details view").
    /// Detects duplicates (same content, whatever the filename), will not reupload anything then.
    /// </summary>
    /// <param name="input">File to upload, as a stream</param>
    /// <param name="hash">SHA-1 hash of the content</param>
    /// <param name="filename">Original filename</param>
    /// <returns>Uploaded URLs</returns>
    public async Task<UploadResult> UploadAsync(Stream input, string hash, string filename)
    {
        string baseOutputName = $"{hash}/{filename}";
        
        // first of all check if it already exists in database (hash is unique there), if so, nothing to do
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

        // might be in storage without being in database (e.g. failed halfway before), reuse what is there then
        string? existingKey = await _pictureStorage.CheckPictureAlreadyUploadedAsync(baseOutputName);
        
        var result = new UploadResult
        {
            ExistedInStorage = existingKey is not null,
            Hash = hash,
            PictureUrl = baseOutputName,
            ThumbnailUrl = baseOutputName,
            DetailsUrl = baseOutputName
        };

        input.Seek(0, SeekOrigin.Begin);
        var content = new byte[input.Length];
        await input.ReadExactlyAsync(content);

        var inputImage = new PictureImage(content);
        int width = inputImage.Width, height = inputImage.Height;
        result.Width = width;
        result.Height = height;
        result.Size = content.Length;

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
        var exif = inputImage.ReadExif();
        DateTime date = DateTime.UtcNow;
        if (exif.DateTimeOriginal != null && exif.DateTimeOriginal != "0000:00:00 00:00:00" &&
            DateTime.TryParseExact(exif.DateTimeOriginal, "yyyy:MM:dd HH:mm:ss",
                CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var exifDate))
        {
            date = exifDate.ToUniversalTime();
            result.PhotographedAtFromExif = true;
        }
        result.PhotographedAt = date;

        // coordinates from EXIF, if any
        result.Lat = exif.Lat;
        result.Lng = exif.Lng;
        
        // camera and lens
        if (exif.Camera != null)
        { 
            result.Camera = exif.Camera;
        }
        result.Lens = GetLensWithoutCamera(exif.Lens, result.Camera);
        
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
    /// Schedules all stored files of a picture version (original and all resized versions) for deletion after
    /// the next publish, e.g. after the picture has been replaced with a new version.  Does not save changes.
    /// </summary>
    public void ScheduleDeletionFromStorage(string hash)
    {
        _context.PendingPictureDeletions.Add(new PendingPictureDeletion { Hash = hash, CreatedAt = DateTime.UtcNow });
    }

    /// <summary>
    /// Deletes files scheduled with <see cref="ScheduleDeletionFromStorage"/>, to be called after publishing.
    /// Skips those that are in use again (a picture replaced back with an earlier version).
    /// </summary>
    /// <returns>Number of picture versions deleted</returns>
    public async Task<int> DeleteScheduledFromStorageAsync()
    {
        int deleted = 0;
        foreach (var pending in await _context.PendingPictureDeletions.ToListAsync())
        {
            if (!await _context.Pictures.AnyAsync(p => p.Hash == pending.Hash))
            {
                await _pictureStorage.DeletePicturesWithPrefixAsync($"{pending.Hash}/");
                deleted++;
            }
            _context.PendingPictureDeletions.Remove(pending);
            await _context.SaveChangesAsync();
        }
        return deleted;
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

        PictureImage? image = null;

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
                    var content = new MemoryStream();
                    await responseStream.CopyToAsync(content);
                    image = new PictureImage(content.ToArray());

                    // pictures uploaded before 3.6.9 have dimensions as stored in the file, without EXIF
                    // orientation applied; correct these now
                    if ((picture.Width, picture.Height) != (image.Width, image.Height))
                    {
                        picture.Width = image.Width;
                        picture.Height = image.Height;
                        (width, height) = picture.GetDownsizedDimensions(targetSize);
                        if (width == 0 || height == 0)
                        {
                            continue;
                        }
                    }
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
        
        return image != null;
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
    /// Resize a picture and upload it, in the format chosen by <see cref="PictureImage.Resize"/>.
    /// </summary>
    /// <returns>Key in storage</returns>
    private async Task<string> UploadDownsizedVersionAsync(PictureImage image, int width, int height, string baseName,
        string suffix, bool clearMetadata)
    {
        var (content, extension) = image.Resize(width, height, clearMetadata);
        var name = GetFilenameWithSuffix(baseName, suffix, extension);
        await _pictureStorage.UploadPictureAsync(name, new MemoryStream(content));
        return name;
    }

    /// <summary>
    /// Some cameras (phones mostly) repeat their own name in the lens name, e.g. "iPhone 6 back camera 4.15mm f/2.2",
    /// drop that.
    /// </summary>
    public static string? GetLensWithoutCamera(string? lens, string? camera)
    {
        if (lens != null && !String.IsNullOrWhiteSpace(camera) &&
            lens.StartsWith(camera, StringComparison.OrdinalIgnoreCase))
        {
            var rest = lens[camera.Length..].TrimStart(' ', '_');
            // unless nothing else is left
            return rest.Length > 0 ? rest : lens;
        }
        return lens;
    }

    private static string GetFilenameWithSuffix(string filename, string suffix, string newExtension)
    {
        string extension = Path.GetExtension(filename);
        return filename.Substring(0, filename.Length - extension.Length) + suffix + newExtension;
    }
}
