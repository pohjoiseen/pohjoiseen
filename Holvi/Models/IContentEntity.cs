namespace Holvi.Models;

public interface IContentEntity
{
    int Id { get; }
    string ContentMD { get; set; }
    int Version { get; set; }
}
