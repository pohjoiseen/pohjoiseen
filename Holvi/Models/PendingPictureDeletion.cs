namespace Holvi.Models;

/// <summary>
/// Files of a replaced picture version (all under "{Hash}/" in storage), to be deleted after the next publish,
/// as the live database still refers to them until then.
/// </summary>
public class PendingPictureDeletion
{
    public int Id { get; set; }

    public string Hash { get; set; } = "";

    public DateTime CreatedAt { get; set; }
}
