using KoTi.ResponseModels;
using Microsoft.AspNetCore.Mvc;

namespace KoTi.Controllers;

[Route("api/Pictures/Upload")]
[ApiController]
public class PicturesUploadController : ControllerBase
{
    private readonly PictureUpload _pictureUpload;

    public PicturesUploadController(PictureUpload pictureUpload)
    {
        _pictureUpload = pictureUpload;
    }

    [HttpPost("{hash}/{filename}")]
    public async Task<ActionResult<UploadResult>> Upload(string hash, string filename)
    {
        // read in image entirely before continuing
        MemoryStream input = new MemoryStream();
        await Request.Body.CopyToAsync(input);
        return await _pictureUpload.UploadFromUIAsync(input, hash, filename);
    }
}