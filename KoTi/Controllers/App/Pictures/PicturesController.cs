using System.Text.Json;
using Holvi;
using Holvi.Models;
using Htmx;
using KoTi.ViewModels.Pictures;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KoTi.Controllers.App.Pictures;

[Route("[controller]")]
public class PicturesController(HolviDbContext dbContext, PictureUpload pictureUpload) : Controller
{
    private const string BrowseComponentId = "browse-pictures";
    private const string UploadPickFolderComponentId = "upload-pick-folder";

    // htmx history restore requests (Back/Forward with no cached snapshot) need the full page, not just the list
    private bool IsHtmxPartialRequest() =>
        Request.IsHtmx() && !Request.Headers.ContainsKey("HX-History-Restore-Request");

    [HttpGet("Folders")]
    public IActionResult BrowseFolders(
        [FromQuery] int? folderId,
        [FromQuery] int offset,
        [FromQuery] string? setSearch)
    {
        var actualFolderId = folderId ?? 0;
        if (IsHtmxPartialRequest())
        {
            return ViewComponent("PictureList", new
            {
                componentId = BrowseComponentId,
                limit = 0,
                offset,
                setId = (int?)actualFolderId,
                setSearch,
                useLinks = true
            });
        }
        ViewData["Mode"] = "folders";
        ViewData["FolderId"] = actualFolderId;
        ViewData["Offset"] = offset;
        ViewData["SetSearch"] = setSearch;
        return View("~/Views/Pictures/Browse.cshtml");
    }

    [HttpGet("All")]
    public IActionResult BrowseAll([FromQuery] int offset)
    {
        if (IsHtmxPartialRequest())
        {
            return ViewComponent("PictureList", new
            {
                componentId = BrowseComponentId,
                limit = 0,
                offset,
                setId = (int?)null,
                setSearch = (string?)null,
                useLinks = true
            });
        }
        ViewData["Mode"] = "all";
        ViewData["Offset"] = offset;
        return View("~/Views/Pictures/Browse.cshtml");
    }

    // Standalone upload, step 1: pick a folder to upload to (or create a new one)
    [HttpGet("Upload")]
    public IActionResult UploadPickFolder([FromQuery] int? folderId, [FromQuery] string? setSearch)
    {
        var actualFolderId = folderId ?? 0;
        if (IsHtmxPartialRequest())
        {
            return UploadPickFolderList(actualFolderId, setSearch);
        }
        ViewData["FolderId"] = actualFolderId;
        ViewData["SetSearch"] = setSearch;
        return View("~/Views/Pictures/UploadPickFolder.cshtml");
    }

    [HttpPost("Upload/CreateFolder")]
    public async Task<IActionResult> UploadCreateFolder([FromQuery] int? parentId, [FromForm] string? name)
    {
        int? actualParentId = parentId > 0 ? parentId : null;
        if (actualParentId is not null && !await dbContext.PictureSets.AnyAsync(ps => ps.Id == actualParentId))
        {
            return Problem(title: "Parent folder not found", statusCode: StatusCodes.Status404NotFound);
        }

        name = name?.Trim();
        if (String.IsNullOrEmpty(name))
        {
            return UploadPickFolderList(actualParentId ?? 0, null);
        }

        // if a folder with this name already exists here, just go into it instead of creating a duplicate
        var set = await dbContext.PictureSets.FirstOrDefaultAsync(ps => ps.ParentId == actualParentId && ps.Name == name);
        if (set is null)
        {
            set = new PictureSet { Name = name, ParentId = actualParentId };
            dbContext.PictureSets.Add(set);
            await dbContext.SaveChangesAsync();
        }

        // navigate straight into the new folder
        Response.Headers["HX-Push-Url"] = Url.Action("UploadPickFolder", new { folderId = set.Id });
        return UploadPickFolderList(set.Id, null);
    }

    private IActionResult UploadPickFolderList(int folderId, string? setSearch)
    {
        return ViewComponent("PictureList", new
        {
            componentId = UploadPickFolderComponentId,
            limit = 0,
            offset = 0,
            setId = (int?)folderId,
            setSearch,
            useLinks = true,
            uploadFolderPicker = true
        });
    }

    // Standalone upload, step 2: upload queue for the picked folder (0 = no folder)
    [HttpGet("Upload/{folderId:int}")]
    public async Task<IActionResult> UploadQueue(int folderId)
    {
        // build full path of the folder for display
        var path = new List<string>();
        int? currentId = folderId > 0 ? folderId : null;
        while (currentId is not null)
        {
            var set = await dbContext.PictureSets.FindAsync(currentId);
            if (set is null)
            {
                return NotFound();
            }
            path.Insert(0, set.Name);
            currentId = set.ParentId;
        }

        ViewData["FolderId"] = folderId;
        ViewData["FolderPath"] = String.Join(" / ", path);
        return View("~/Views/Pictures/UploadQueue.cshtml");
    }

    [HttpGet("List/{componentId}")]
    public async Task<IActionResult> List(
        string componentId,
        [FromQuery] int limit,
        [FromQuery] int offset,
        [FromQuery] int? setId,
        [FromQuery] string? setSearch)
    {
        return ViewComponent("PictureList", new { componentId, limit, offset, setId, setSearch });
    }
    
    [HttpGet("Picker/{componentId}/{fieldName}")]
    public async Task<IActionResult> Picker(
        string componentId,
        string fieldName,
        [FromQuery] int? pictureId)
    {
        Response.Headers["HX-Trigger-After-Swap"] = JsonSerializer.Serialize(new Dictionary<string, dynamic>
        {
            { "picture-picker:loaded", new { componentId, pictureId } }
        });
        return ViewComponent("PicturePicker", new { componentId, fieldName, pictureId });
    }

    [HttpGet("Fullscreen/{componentId}/{pictureId:int}")]
    public async Task<IActionResult> Fullscreen(string componentId, int pictureId,
            [FromQuery] int limit,
            [FromQuery] int offset,
            [FromQuery] int? setId,
            [FromQuery] string? setSearch,
            [FromQuery] string? overrideIds,
            [FromQuery] bool useLinks = false)
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
                offset -= limit > 0 ? limit : PictureListViewComponent.DefaultLimit;
            }

            if (pictureIdIndex >= offset + limit)
            {
                changePage = componentId;
                offset += limit > 0 ? limit : PictureListViewComponent.DefaultLimit;
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
            OverrideIds = overrideIds,
            UseLinks = useLinks
        });
    }
    
    // onConflict: what to do if the target folder already has a picture with the same filename (but different
    // content): "replace" it, keeping its id; "rename" this one to filename-1.jpg etc.; otherwise 409 is returned
    [HttpPost("Upload/{hash}/{filename}")]
    public async Task<IActionResult> Upload(string hash, string filename, [FromQuery] string? setName,
        [FromQuery] int? setId, [FromQuery] string? onConflict)
    {
        // target set can be specified by id (must exist), or by name, see below; or not at all
        if (setId is not null && !await dbContext.PictureSets.AnyAsync(ps => ps.Id == setId))
        {
            return Problem(title: "Target folder not found", statusCode: StatusCodes.Status400BadRequest);
        }

        // find or create top-level set by name if necessary
        if (setId is null && setName is not null)
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

        // new version of a picture already in this folder?  (exactly the same content is a duplicate instead,
        // detected below)
        Picture? replaced = null;
        if (!await dbContext.Pictures.AnyAsync(p => p.Hash == hash))
        {
            var sameName = await dbContext.Pictures
                .Where(p => p.SetId == setId && p.Filename == filename)
                .OrderBy(p => p.Id)
                .FirstOrDefaultAsync();
            if (sameName is not null)
            {
                switch (onConflict)
                {
                    case "replace":
                        replaced = sameName;
                        break;
                    case "rename":
                        filename = await GetFreeFilenameAsync(setId, filename);
                        break;
                    default:
                        return Problem(title: $"{filename} already exists in this folder",
                            statusCode: StatusCodes.Status409Conflict);
                }
            }
        }

        // upload (which detects dups) and get uploaded image data
        var uploadResult = await pictureUpload.UploadAsync(input, hash, filename);

        // retrieve picture if already existed, otherwise create new or update the replaced one
        Picture picture;
        if (uploadResult.ExistingId is not null)
        {
            picture = (await dbContext.Pictures.FindAsync(uploadResult.ExistingId))!;
        }
        else if (replaced is not null)
        {
            picture = replaced;
            var oldHash = picture.Hash;
            picture.Hash = hash;
            picture.Url = uploadResult.PictureUrl;
            picture.ThumbnailUrl = uploadResult.ThumbnailUrl;
            picture.DetailsUrl = uploadResult.DetailsUrl;
            picture.UploadedAt = DateTime.UtcNow;
            picture.Width = uploadResult.Width;
            picture.Height = uploadResult.Height;
            picture.Size = uploadResult.Size;
            // edited versions may have lost some of EXIF, keep what the old one had then
            if (uploadResult.PhotographedAtFromExif)
            {
                picture.PhotographedAt = uploadResult.PhotographedAt;
            }
            if (uploadResult.Lat is not null && uploadResult.Lng is not null)
            {
                picture.Lat = uploadResult.Lat;
                picture.Lng = uploadResult.Lng;
            }
            if (!String.IsNullOrEmpty(uploadResult.Camera))
            {
                picture.Camera = uploadResult.Camera;
                picture.Lens = uploadResult.Lens;
            }
            // website sizes get recreated for the new version when needed
            picture.WebsiteSizesExist = false;
            picture.Website1xUrl = null;
            picture.Website2xUrl = null;
            // files of the old version are still used by the live site, delete them only after next publish
            pictureUpload.ScheduleDeletionFromStorage(oldHash);
            await dbContext.SaveChangesAsync();
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
            IsDuplicate = uploadResult.ExistingId is not null,
            IsReplaced = replaced is not null
        });
    }

    // photo.jpg -> photo-1.jpg, photo-2.jpg, ..., first one not yet in the folder
    private async Task<string> GetFreeFilenameAsync(int? setId, string filename)
    {
        var stem = Path.GetFileNameWithoutExtension(filename);
        var extension = Path.GetExtension(filename);
        var existing = (await dbContext.Pictures
            .Where(p => p.SetId == setId && p.Filename.StartsWith(stem + "-"))
            .Select(p => p.Filename)
            .ToListAsync()).ToHashSet();
        for (int i = 1;; i++)
        {
            var candidate = $"{stem}-{i}{extension}";
            if (!existing.Contains(candidate))
            {
                return candidate;
            }
        }
    }
}
