using Holvi;
using Holvi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KoTi.Controllers.API;

/// <summary>
/// History of the text of posts, articles and books, see <see cref="Revision"/>.  Read-only.
/// </summary>
[Route("api/Revisions")]
[ApiController]
public class RevisionsApiController(HolviDbContext dbContext) : ControllerBase
{
    public record RevisionSummary(int Id, string TargetType, int TargetId, int Version, int ContentLength,
        DateTime CreatedAt, DateTime SavedAt, bool Keep);

    public record RevisionResponse(int Id, string TargetType, int TargetId, int Version, int ContentLength,
        DateTime CreatedAt, DateTime SavedAt, bool Keep, string ContentMD);

    // GET: api/Revisions?targetType=post&targetId=5
    // Without texts, oldest first.
    [HttpGet]
    public async Task<ActionResult<IList<RevisionSummary>>> GetRevisions([FromQuery] string targetType, [FromQuery] int targetId)
    {
        return await dbContext.Revisions
            .Where(r => r.TargetType == targetType && r.TargetId == targetId)
            .OrderBy(r => r.Id)
            .Select(r => new RevisionSummary(r.Id, r.TargetType, r.TargetId, r.Version, r.ContentLength, r.CreatedAt, r.SavedAt, r.Keep))
            .ToListAsync();
    }

    // GET: api/Revisions/7
    [HttpGet("{id}")]
    public async Task<ActionResult<RevisionResponse>> GetRevision(int id)
    {
        var r = await dbContext.Revisions.FindAsync(id);
        if (r is null)
        {
            return NotFound();
        }

        return new RevisionResponse(r.Id, r.TargetType, r.TargetId, r.Version, r.ContentLength, r.CreatedAt, r.SavedAt, r.Keep, r.GetContent());
    }
}
