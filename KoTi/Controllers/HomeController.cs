using System.Diagnostics;
using Holvi;
using KoTi.ResponseModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace KoTi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class HomeController(HolviDbContext dbContext, IConfiguration configuration) : ControllerBase
{
    // GET: api/Home/Stats
    [HttpGet("Stats")]
    public async Task<ActionResult<Stats>> GetStats()
    {
        var dbFileInfo = new FileInfo(configuration["KoTi:LiveDatabase"]!);
        return new Stats
        {
            TotalPictures = await dbContext.Pictures.CountAsync(),
            TotalPicturesWithNoLocation = await dbContext.Pictures.Where(p => p.PlaceId == null).CountAsync(),
            TotalPlaces = await dbContext.Places.CountAsync(),
            TotalPosts = await dbContext.Posts.CountAsync(),
            TotalArticles = await dbContext.Articles.CountAsync(),
            DatabaseLastPublishedAt = dbFileInfo.LastWriteTime,
            DatabaseSize = dbFileInfo.Length,
            S3Bucket = $"{configuration["Holvi:S3:Bucket"]!} ({configuration["Holvi:S3:PublicURL"]!.Replace("https://", "").Replace("/", "")})"
        };
    }

    // POST: api/Home/Publish
    [HttpPost("Publish")]
    public async Task<IActionResult> Publish()
    {
        SqliteConnection.ClearAllPools();  // flushes unsaved data
        // ensure database is not modified while being copied
        await dbContext.Database.ExecuteSqlAsync($"BEGIN EXCLUSIVE");
        // execute the publish script, wait for completion and capture its output
        var psi = new ProcessStartInfo
        {
            FileName = "/bin/sh",
            ArgumentList = { "-c", configuration["KoTi:PublishCommand"]! },
            UseShellExecute = false,  // a bit confusing when we're running /bin/sh :)
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        var process = new Process { StartInfo = psi };
        process.Start();
        await process.WaitForExitAsync();
        var result = new
        {
            exitCode = process.ExitCode,
            stdout = await process.StandardOutput.ReadToEndAsync(),
            stderr = await process.StandardError.ReadToEndAsync()
        };
        await dbContext.Database.ExecuteSqlAsync($"COMMIT");
        return Ok(result);
    }
}