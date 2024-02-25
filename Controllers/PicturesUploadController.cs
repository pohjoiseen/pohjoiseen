using KoTi.Models;
using KoTi.ResponseModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace KoTi.Controllers;

[Route("api/Pictures/Upload")]
[ApiController]
public class PicturesUploadController : ControllerBase
{
    private readonly PictureStorage _pictureStorage;
    private readonly KoTiDbContext _context;

    public PicturesUploadController(PictureStorage pictureStorage, KoTiDbContext context)
    {
        _pictureStorage = pictureStorage;
        _context = context;
    }
    
    [HttpPost("{hash}/{filename}")]
    public async Task<ActionResult<UploadResult>> Upload(string hash, string filename)
    {
        // read in image entirely before continuing
        MemoryStream input = new MemoryStream();
        await Request.Body.CopyToAsync(input);
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
                ExistingId = existing?.Id,
                Hash = hash,
                PictureUrl = _pictureStorage.PublicUrl + existingKey,
                ThumbnailUrl = _pictureStorage.PublicUrl + GetFilenameWithSuffix(existingKey, Picture.ThumbnailSuffix),
                DetailsUrl = _pictureStorage.PublicUrl + GetFilenameWithSuffix(existingKey, Picture.DetailsSuffix)
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

                string outputName = GetFilenameWithSuffix(baseOutputName, Picture.ThumbnailSuffix);
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
                    await resizedImage.SaveAsync(output, new JpegEncoder());
                }

                string outputName = GetFilenameWithSuffix(baseOutputName, Picture.DetailsSuffix);
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

    private string GetFilenameWithSuffix(string filename, string suffix)
    {
        string extension = Path.GetExtension(filename);
        return filename.Substring(0, filename.Length - extension.Length) + suffix + ".jpg";
    }
}