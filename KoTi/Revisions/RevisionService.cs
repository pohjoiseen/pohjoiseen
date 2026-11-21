using Holvi;
using Holvi.Models;

namespace KoTi.Revisions;

/// <summary>
/// History of content for the edit pages, see <see cref="RevisionRecorder"/>.
/// </summary>
public class RevisionService(HolviDbContext dbContext)
{
    public Task<List<Revision>> ListAsync(string targetType, int targetId) =>
        RevisionRecorder.ListAsync(dbContext, targetType, targetId);
}
