using Holvi;
using Holvi.Models;
using Microsoft.EntityFrameworkCore;

namespace KoTi.ModelFactories;

public static class ContentVersioning
{
    /// <summary>
    /// Saves all changes, but only if the content entity still has the expected version in the database
    /// (optimistic locking); the version is incremented.  Otherwise nothing is saved, all tracked changes
    /// are dropped and DbUpdateConcurrencyException is thrown.
    /// </summary>
    public static async Task SaveVersionedAsync(this HolviDbContext dbContext, IContentEntity entity, int expectedVersion)
    {
        // Version is a concurrency token, so this becomes UPDATE ... WHERE Version = expectedVersion
        var version = dbContext.Entry(entity).Property(nameof(IContentEntity.Version));
        version.OriginalValue = expectedVersion;
        version.CurrentValue = expectedVersion + 1;
        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            dbContext.ChangeTracker.Clear();
            throw;
        }
    }
}
