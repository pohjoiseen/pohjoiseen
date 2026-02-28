using System.Diagnostics;
using System.Reflection;
using Holvi;
using KoTi.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace KoTi.Controllers.App;

public class HomeController(HolviDbContext dbContext, IConfiguration configuration) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> GetStats()
    {
        var dbFileInfo = new FileInfo(configuration["KoTi:LiveDatabase"]!);
        return View("~/Views/Home.cshtml", new HomeViewModel
        {
            TotalPictures = await dbContext.Pictures.CountAsync(),
            TotalPosts = await dbContext.Posts
                .GroupBy(p => p.Language)
                .Select(g => new { Language = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.Language, g => g.Count),
            TotalArticles = await dbContext.Articles
                .GroupBy(p => p.Language)
                .Select(g => new { Language = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.Language, g => g.Count),
            TotalBooks = await dbContext.Books
                .GroupBy(p => p.Language)
                .Select(g => new { Language = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.Language, g => g.Count),
            Version = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion,
            DatabaseLastPublishedAt = dbFileInfo.LastWriteTime,
            DatabaseSize = dbFileInfo.Length,
            S3Bucket =
                $"{configuration["Holvi:S3:Bucket"]!} ({configuration["Holvi:S3:PublicURL"]!.Replace("https://", "").Replace("/", "")})"
        });
    }
    
    [HttpGet("app/Publish")]
    [HttpPost("app/Publish")]
    public async Task<IActionResult> Publish()
    {
        var dbFileInfo = new FileInfo(configuration["KoTi:LiveDatabase"]!);
        ViewBag.DatabaseLastPublishedAt = dbFileInfo.LastWriteTime;

        if (Request.Method == "POST")
        {
            SqliteConnection.ClearAllPools(); // flushes unsaved data
            // ensure database is not modified while being copied
            await dbContext.Database.ExecuteSqlAsync($"BEGIN EXCLUSIVE");
            // execute the publish script, wait for completion and capture its output
            var psi = new ProcessStartInfo
            {
                FileName = "/bin/sh",
                ArgumentList = { "-c", configuration["KoTi:PublishCommand"]! },
                UseShellExecute = false, // a bit confusing when we're running /bin/sh :)
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            var process = new Process { StartInfo = psi };
            process.Start();
            await process.WaitForExitAsync();
            await dbContext.Database.ExecuteSqlAsync($"COMMIT");
            ViewBag.ExitCode = process.ExitCode;
            ViewBag.Stdout = await process.StandardOutput.ReadToEndAsync();
            ViewBag.Stderr = await process.StandardError.ReadToEndAsync();
        }

        return View("~/Views/Publish.cshtml");
    }
    
    [HttpGet("app/Search")]
    public async Task<ActionResult<SearchResultsViewModel>> Search([FromQuery] string q, [FromQuery] int offset)
    {
        const int limit = 25;

        var results = new SearchResultsViewModel
        {
            Query = q,
            Results = new List<SearchResultsViewModel.SearchResultViewModel>(),
            Total = 0,
            Limit = limit,
            Offset = offset
        };
        
        if (!string.IsNullOrWhiteSpace(q))
        {
            var query = dbContext.Database
                .SqlQuery<SearchResultsViewModel.SearchResultViewModel>(
                    $"SELECT TableName, TableId, Title, snippet(Search, 3, '<b>', '</b>', '...', 25) AS Text, bm25(Search, 1.0, 1.0, 3.0) AS Rank FROM Search({q}) WHERE TableName IN ('Posts', 'Articles', 'Books')");
            try
            {
                results.Total = await query.CountAsync();
                results.Results = await query
                    .OrderBy(r => r.Rank)
                    .Skip(offset)
                    .Take(limit)
                    .ToListAsync();
            }
            catch (SqliteException e)
            {
                results.Error = e.Message;
            }

            foreach (var result in results.Results)
            {
                
            }
        }
        
        return View("~/Views/Search.cshtml", results);
    }

    [HttpGet("app/Blank")]
    public IActionResult Blank() => Ok("");
}