using Holvi;
using Holvi.Models;
using KoTi.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KoTi.Controllers.App;

[Route("app/[controller]")]
public class PicturesController(HolviDbContext dbContext, PictureUpload pictureUpload) : Controller
{
    [HttpGet("Picker/{componentId}")]
    public async Task<IActionResult> Picker(
        string componentId,
        [FromQuery] int limit,
        [FromQuery] int offset,
        [FromQuery] int? setId,
        [FromQuery] string? setSearch)
    {
        return ViewComponent("PicturesPicker", new { componentId, limit, offset, setId, setSearch });
    }
    
    [HttpGet("Fullscreen/{componentId}/{pictureId:int}")]
    public async Task<IActionResult> Fullscreen(string componentId, int pictureId,
            [FromQuery] int limit,
            [FromQuery] int offset,
            [FromQuery] int? setId,
            [FromQuery] string? setSearch,
            [FromQuery] string? overrideIds)
    {
        IList<int> pictureIds;
     
        if (overrideIds is not null)
        {
            // allow to specify all ids manually as a comma-separated list
            // used in uploader
            pictureIds = overrideIds.Split(',').Select(int.Parse).ToList();
        }
        else
        {
            // otherwise, get ids of all pictures in this set
            var query = dbContext.Pictures
                .Include(p => p.Set)
                .AsQueryable();

            query = query.OrderBy(p => p.PhotographedAt);
            if (setId != null)
            {
                if (setId > 0)
                {
                    query = query.Where(p => p.SetId == setId);
                }
                else
                {
                    query = query.Where(p => p.SetId == null);
                }
            }

            pictureIds = await query.Select(p => p.Id).ToListAsync();
        }

        // find position of the current picture among all in the list
        var pictureIdIndex = pictureIds.IndexOf(pictureId);
        if (pictureIdIndex == -1)
        {
            return NotFound();
        }
        
        // pick a few adjacent pictures to preload
        var preloadPictureIds = new List<int>();
        for (int i = pictureIdIndex - 3; i < pictureIdIndex + 3; i++)
        {
            if (i != pictureIdIndex && i >= 0 && i < pictureIds.Count)
            {
                preloadPictureIds.Add(pictureIds[i]);
            }
        }

        // see if we need to change page
        string? changePage = null;
        if (overrideIds is null)
        {
            if (pictureIdIndex < offset)
            {
                changePage = componentId;
                offset -= limit > 0 ? limit : PicturesPickerViewComponent.DefaultLimit;
            }

            if (pictureIdIndex >= offset + limit)
            {
                changePage = componentId;
                offset += limit > 0 ? limit : PicturesPickerViewComponent.DefaultLimit;
            }
        }

        return View(new FullscreenViewModel
        {
            Picture = (await dbContext.Pictures.FindAsync(pictureId))!,
            PreviousId = pictureIdIndex > 0 ? pictureIds[pictureIdIndex - 1] : -1,
            NextId = pictureIdIndex < pictureIds.Count - 1 ? pictureIds[pictureIdIndex + 1] : -1,
            PreloadPictureUrls = await dbContext.Pictures.Where(p => preloadPictureIds.Contains(p.Id))
                .Select(p => p.Url).ToListAsync(),
            SetId = setId,
            SetSearch = setSearch,
            Limit = limit,
            Offset = offset,
            Total = pictureIds.Count,
            ChangePage = changePage,
            OverrideIds = overrideIds
        });
    }
    
    [HttpPost("Upload/{hash}/{filename}")]
    public async Task<IActionResult> Upload(string hash, string filename, [FromQuery] string? setName)
    {
        // find or create top-level set by name if necessary
        int? setId = null;
        if (setName is not null)
        {
            var set = dbContext.PictureSets.FirstOrDefault(s => s.Name == setName);
            if (set is null)
            {
                set = new PictureSet { Name = setName, IsPrivate = true };
                dbContext.PictureSets.Add(set);
                await dbContext.SaveChangesAsync();
            }

            setId = set.Id;
        }

        // read in image entirely before continuing
        MemoryStream input = new MemoryStream();
        await Request.Body.CopyToAsync(input);

        // upload (which detects dups) and get uploaded image data
        var uploadResult = await pictureUpload.UploadAsync(input, hash, filename);

        // retrieve picture if already existed, otherwise create new
        Picture picture;
        if (uploadResult.ExistingId is not null)
        {
            picture = (await dbContext.Pictures.FindAsync(uploadResult.ExistingId))!;
        }
        else
        {
            picture = new Picture
            {
                Filename = filename,
                Hash = hash,
                Url = uploadResult.PictureUrl,
                ThumbnailUrl = uploadResult.ThumbnailUrl,
                DetailsUrl = uploadResult.DetailsUrl,
                UploadedAt = DateTime.UtcNow,
                Width = uploadResult.Width,
                Height = uploadResult.Height,
                Size = uploadResult.Size,
                PhotographedAt = uploadResult.PhotographedAt,
                Camera = uploadResult.Camera,
                Lens = uploadResult.Lens,
                Lat = uploadResult.Lat,
                Lng = uploadResult.Lng,
                SetId = setId
            };
            dbContext.Pictures.Add(picture);
            await dbContext.SaveChangesAsync();
        }

        // return minimal data as JSON
        return Json(new
        {
            picture.Id,
            Title = $"{picture.PhotographedAt?.ToString("d.MM.yyyy")}\n{picture.Filename}",
            Src = picture.DetailsUrl,
            FullscreenUrl = Url.Action("Fullscreen", new { componentId = "_Upload", pictureId = picture.Id }),
            IsDuplicate = uploadResult.ExistingId is not null
        });
    }
}