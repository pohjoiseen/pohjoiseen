namespace Teos;

/// <summary>
/// Static content processor.  These build one or more output files from input files
/// and put them into output dir.  This is done always, even in server mode, as they are served
/// statically even then.  Output files are not rebuilt if they (all) exist and are not older than
/// input files.
/// </summary>
public interface IStaticProcessor : ITeosEngineAware
{
    /// <summary>
    /// Can this processor in its current configuration handle a particular file?
    /// </summary>
    /// <param name="path">Relative filepath to check against</param>
    /// <returns>Yes/No</returns>
    public bool Match(string path);

    /// <summary>
    /// Does the output for this processor for this file need to be regenerated?
    /// </summary>
    /// <param name="path">Relative path to input file</param>
    /// <returns>Yes/no</returns>
    public bool IsOutputStale(string path);

    /// <summary>
    /// Build output file(s) from an input file, asyncronously.
    /// </summary>
    /// <param name="path">Relative path to input file</param>
    /// <returns>Async task</returns>
    public Task Output(string path);
}