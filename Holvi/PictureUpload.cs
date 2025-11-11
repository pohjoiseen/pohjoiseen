using Holvi.Models;
using Holvi.ResponseModels;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;

namespace Holvi;

public class PictureUpload
{
    private readonly PictureStorage _pictureStorage;
    private readonly HolviDbContext _context;

    private const int JpegQualityLevel = 75;

    public PictureUpload(PictureStorage pictureStorage, HolviDbContext context)
    {
        _pictureStorage = pictureStorage;
        _context = context;
    }

    public async Task<UploadResult> UploadAsync(Stream input, string hash, string filename)
    {
        string baseOutputName = $"{hash}/{filename}";

        // do not do anything if hash already exists
        // we wouldn't even have needed to wait until the rest of the upload, but that's basically necessary :(
        string? existingKey = await _pictureStorage.CheckPictureAlreadyUploadedAsync(baseOutputName);
        if (existingKey != null)
        {
            var existing = await _context
                .Pictures
                .Where(p => p.Hash == hash).FirstOrDefaultAsync();
            
            return new UploadResult
            {
                ExistedInStorage = true,
                ExistingId = existing?.Id,
                Hash = hash,
                PictureUrl = _pictureStorage.PublicUrl + existingKey,
                ThumbnailUrl = _pictureStorage.PublicUrl + GetFilenameWithSuffix(existingKey, Picture.ThumbnailSuffix, true),
                DetailsUrl = _pictureStorage.PublicUrl + GetFilenameWithSuffix(existingKey, Picture.DetailsSuffix, true)
            };
        }
        
        var result = new UploadResult
        {
            Hash = hash,
            PictureUrl = baseOutputName,
            ThumbnailUrl = baseOutputName,
            DetailsUrl = baseOutputName
        };

        // downscaled versions
        input.Seek(0, SeekOrigin.Begin);
        using (var inputImage = await Image.LoadAsync(input))
        {
            // thumbnails
            // always match height, resize width as important
            // if height is no bigger than target size, do not do anything else, base filename will be used
            int width = inputImage.Width, height = inputImage.Height;
            if (height > Picture.ThumbnailSize * 2)
            {
                double scale = Picture.ThumbnailSize * 2.0 / height;
                MemoryStream output = new MemoryStream();
                using (var resizedImage = inputImage.Clone(x => x.Resize((int) (width * scale), Picture.ThumbnailSize * 2)))
                {
                    await resizedImage.SaveAsync(output, new JpegEncoder());
                }

                string outputName = GetFilenameWithSuffix(baseOutputName, Picture.ThumbnailSuffix, true);
                output.Seek(0, SeekOrigin.Begin);
                await _pictureStorage.UploadPictureAsync(outputName, output);
                result.ThumbnailUrl = outputName;
            }
            
            // details
            // same but match width
            if (width > Picture.DetailsSize * 2)
            {
                double scale = Picture.DetailsSize * 2.0 / width;
                MemoryStream output = new MemoryStream();
                using (var resizedImage = inputImage.Clone(x => x.Resize(Picture.DetailsSize * 2, (int) (height * scale))))
                {
                    await resizedImage.SaveAsync(output, new JpegEncoder { Quality = JpegQualityLevel });
                }

                string outputName = GetFilenameWithSuffix(baseOutputName, Picture.DetailsSuffix, true);
                output.Seek(0, SeekOrigin.Begin);
                await _pictureStorage.UploadPictureAsync(outputName, output);
                result.DetailsUrl = outputName;
            }
        }
        
        // upload the original image last
        input.Seek(0, SeekOrigin.Begin);
        await _pictureStorage.UploadPictureAsync(baseOutputName, input);

        result.DetailsUrl = _pictureStorage.PublicUrl + result.DetailsUrl;
        result.ThumbnailUrl = _pictureStorage.PublicUrl + result.ThumbnailUrl;
        result.PictureUrl = _pictureStorage.PublicUrl + result.PictureUrl;
        return result;
    }

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
