using Holvi;
using Holvi.ResponseModels;
using KoTi.ResponseModels;
using Microsoft.AspNetCore.Mvc;

namespace KoTi.Controllers.API;

[Route("api/Pictures/Upload")]
[ApiController]
public class PicturesUploadApiController : ControllerBase
{
    private readonly PictureUpload _pictureUpload;

    public PicturesUploadApiController(PictureUpload pictureUpload)
    {
        _pictureUpload = pictureUpload;
    }

    [HttpPost("{hash}/{filename}")]
    public async Task<ActionResult<UploadResult>> Upload(string hash, string filename)
    {
        // read in image entirely before continuing
        MemoryStream input = new MemoryStream();
        await Request.Body.CopyToAsync(input);
        return await _pictureUpload.UploadAsync(input, hash, filename);
    }
}