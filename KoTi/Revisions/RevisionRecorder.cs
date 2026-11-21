using Holvi;
using Holvi.Models;
using Microsoft.EntityFrameworkCore;

namespace KoTi.Revisions;

/// <summary>
/// Keeps history of content (see <see cref="Revision"/>) without a revision for every save, as the edit page
/// is saved very often while writing:
/// <list type="bullet">
/// <item>saves which do not change the text record nothing;</item>
/// <item>saves within an hour of the start of the latest revision go into that revision (overwrite it);</item>
/// <item>of revisions older than a month only the last one of each day is left;</item>
/// <item>revisions marked to be kept (published text) are never overwritten or removed.</item>
/// </list>
/// The text as it was before the very first recorded save is kept as a revision too.
/// </summary>
public static class RevisionRecorder
{
    public static readonly TimeSpan Window = TimeSpan.FromHours(1);
    public static readonly TimeSpan ThinAfter = TimeSpan.FromDays(30);

    /// <summary>
    /// Adds changes of revisions for the content entity about to be saved to the context, without saving.
    /// </summary>
    public static async Task RecordAsync(HolviDbContext dbContext, IContentEntity entity, int newVersion, DateTime? now = null)
    {
        var entry = dbContext.Entry(entity);
        var content = entity.ContentMD;
        var originalContent = (string)entry.Property(nameof(IContentEntity.ContentMD)).OriginalValue!;
        if (content == originalContent)
        {
            return;
        }

        var time = now ?? DateTime.UtcNow;
        var targetType = Suggestion.TargetTypeOf(entity);
        // without texts
        var existing = await dbContext.Revisions
            .Where(r => r.TargetType == targetType && r.TargetId == entity.Id)
            .OrderBy(r => r.Id)
            .Select(r => new { r.Id, r.CreatedAt, r.SavedAt, r.Keep })
            .ToListAsync();

        if (existing.Count == 0)
        {
            // history starts with what there was before
            var originalSavedAt = entry.Metadata.FindProperty("UpdatedAt") is not null
                ? (DateTime)entry.Property("UpdatedAt").OriginalValue!
                : time;
            var original = new Revision
            {
                TargetType = targetType, TargetId = entity.Id, Version = newVersion - 1,
                CreatedAt = originalSavedAt, SavedAt = originalSavedAt, Keep = true
            };
            original.SetContent(originalContent);
            dbContext.Revisions.Add(original);
        }

        var latest = existing.LastOrDefault();
        if (latest is not null && !latest.Keep && time - latest.CreatedAt < Window)
        {
            var revision = await dbContext.Revisions.SingleAsync(r => r.Id == latest.Id);
            revision.SetContent(content);
            revision.Version = newVersion;
            revision.SavedAt = time;
        }
        else
        {
            var revision = new Revision
            {
                TargetType = targetType, TargetId = entity.Id, Version = newVersion, CreatedAt = time, SavedAt = time
            };
            revision.SetContent(content);
            dbContext.Revisions.Add(revision);
        }

        // thin out old ones: the last of each day stays
        var old = existing.Where(r => !r.Keep && time - r.SavedAt > ThinAfter).ToList();
        foreach (var revision in old.GroupBy(r => r.SavedAt.Date).SelectMany(day => day.OrderBy(r => r.Id).SkipLast(1)))
        {
            dbContext.Revisions.Remove(new Revision { Id = revision.Id });
        }
    }

    /// <summary>
    /// Revisions of a post/article/book without their texts, oldest first.
    /// </summary>
    public static async Task<List<Revision>> ListAsync(HolviDbContext dbContext, string targetType, int targetId) =>
        await dbContext.Revisions
            .Where(r => r.TargetType == targetType && r.TargetId == targetId)
            .OrderBy(r => r.Id)
            .Select(r => new Revision
            {
                Id = r.Id, TargetType = r.TargetType, TargetId = r.TargetId, Version = r.Version,
                ContentLength = r.ContentLength, CreatedAt = r.CreatedAt, SavedAt = r.SavedAt, Keep = r.Keep
            })
            .ToListAsync();

    /// <summary>
    /// Marks the current revision of everything as kept, to be called after publishing.
    /// </summary>
    public static async Task<int> KeepLatestAsync(HolviDbContext dbContext) =>
        await dbContext.Database.ExecuteSqlRawAsync(
            "UPDATE Revisions SET Keep = 1 WHERE Keep = 0 AND Id IN (SELECT MAX(Id) FROM Revisions GROUP BY TargetType, TargetId)");

    /// <summary>
    /// Removes all revisions of a deleted post/article/book, without saving.
    /// </summary>
    public static async Task RemoveForTargetAsync(HolviDbContext dbContext, string targetType, int targetId)
    {
        var ids = await dbContext.Revisions
            .Where(r => r.TargetType == targetType && r.TargetId == targetId)
            .Select(r => r.Id)
            .ToListAsync();
        foreach (var id in ids)
        {
            dbContext.Revisions.Remove(new Revision { Id = id });
        }
    }
}
