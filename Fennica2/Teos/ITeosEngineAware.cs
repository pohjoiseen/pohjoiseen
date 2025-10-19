namespace Teos;

/// <summary>
/// Base interface for interfaces which use a back reference to TeosEngine.
/// </summary>
public interface ITeosEngineAware
{ 
    /// <summary>
    /// Sets the engine instance.
    /// </summary>
    /// <param name="teosEngine">Engine instance</param>
    public void SetTeosEngine(ITeosEngine teosEngine) { }
}
