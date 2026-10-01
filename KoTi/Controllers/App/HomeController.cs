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
    
    [HttpGet("Publish")]
    [HttpPost("Publish")]
    public async Task<IActionResult> Publish()
    {
        if (Request.Method == "POST")
        {
            SqliteConnection.ClearAllPools(); // flushes unsaved data
            // ensure database is not modified while being copied: hold an exclusive lock on a dedicated
            // connection (EF opens/closes pooled connections per command, so a lock taken through it is not kept)
            var connectionString = new SqliteConnectionStringBuilder(dbContext.Database.GetConnectionString())
            {
                Pooling = false
            }.ToString();
            await using var lockConnection = new SqliteConnection(connectionString);
            await lockConnection.OpenAsync();
            await using (var lockCommand = lockConnection.CreateCommand())
            {
                lockCommand.CommandText = "BEGIN EXCLUSIVE";
                await lockCommand.ExecuteNonQueryAsync();
            }

            try
            {
                // execute the publish script, wait for completion and capture its output
                var psi = new ProcessStartInfo
                {
                    FileName = "/bin/sh",
                    ArgumentList = { "-c", configuration["KoTi:PublishCommand"]! },
                    UseShellExecute = false, // a bit confusing when we're running /bin/sh :)
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using var process = new Process { StartInfo = psi };
                process.Start();
                // read output while the process runs, otherwise it blocks once pipe buffers are full
                var stdoutTask = process.StandardOutput.ReadToEndAsync();
                var stderrTask = process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();
                ViewBag.ExitCode = process.ExitCode;
                ViewBag.Stdout = await stdoutTask;
                ViewBag.Stderr = await stderrTask;
            }
            finally
            {
                await using var unlockCommand = lockConnection.CreateCommand();
                unlockCommand.CommandText = "ROLLBACK"; // nothing was written anyway
                await unlockCommand.ExecuteNonQueryAsync();
            }
        }

        var dbFileInfo = new FileInfo(configuration["KoTi:LiveDatabase"]!);
        ViewBag.DatabaseLastPublishedAt = dbFileInfo.LastWriteTime;

        return View("~/Views/Publish.cshtml");
    }
    
    [HttpGet("Search")]
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

    [HttpGet("Blank")]
    public IActionResult Blank() => Ok("");
}