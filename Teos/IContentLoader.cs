namespace Teos;

/// <summary>
/// Loads (deserializes) a content file into an instance of a class inheriting from Content.
/// </summary>
public interface IContentLoader : ITeosEngineAware
{
    /// <summary>
    /// Loads (deserializes) a content item.
    /// </summary>
    /// <param name="filename">Filetname to load from</param>
    /// <param name="stream">Already opened stream to load from</param>
    /// <param name="type">Expected type (descends to Content)</param>
    /// <returns></returns>
    public Task<Content> LoadContent(string filename, Stream stream, Type type);
}