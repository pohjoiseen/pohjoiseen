using System.Text.RegularExpressions;

namespace Teos;

/// <summary>
/// Simply copies files from a specified input to a specified output dir, optionally with some excluded.
/// Recreates entire directory structure.
/// </summary>
public class CopyFileProcessor : IStaticProcessor
{
    private readonly string _inputDir;
    private readonly string _outputDir;
    private readonly string[] _excludeRegexps;

    private ITeosEngine _teosEngine;
    
    public void SetTeosEngine(ITeosEngine teosEngine)
    {
        _teosEngine = teosEngine;
    }

    /// <summary>
    /// CopyFileProcessor constructor.
    /// </summary>
    /// <param name="inputDir">Relative path to input directory</param>
    /// <param name="outputDir">Relative path to output directory, default to same as input</param>
    /// <param name="excludeRegexps">Optional regexp patterns to exclude (applied to entire paths, not just filenames)</param>
    public CopyFileProcessor(string inputDir, string outputDir = null, string[] excludeRegexps = null)
    {
        _inputDir = inputDir;
        _outputDir = outputDir ?? inputDir;
        _excludeRegexps = excludeRegexps;
    }

    /// <summary>
    /// Can this processor in its current configuration handle a particular file?
    /// </summary>
    /// <param name="path">Relative filepath to check against</param>
    /// <returns>Yes/No</returns>
    public bool Match(string path)
    {
        // must be in input dir
        if (_inputDir.Length > 0 &&  !path.StartsWith(_inputDir + "/"))
        {
            return false;
        }

        // must not match exclude regexps, if any
        if (_excludeRegexps != null)
        {
            foreach (string excludeRegexp in _excludeRegexps)
            {
                if (Regex.IsMatch(path, excludeRegexp)) { return false; }
            }
        }

        return true;
    }

    /// <summary>
    /// Does the output for this processor for this file need to be regenerated?
    /// </summary>
    /// <param name="path">Relative path to input file</param>
    /// <returns>Yes/no</returns>
    public bool IsOutputStale(string path)
    {
        // check if mtime of original file is newer than the copy
        // (File.GetLastWriteTimeUtc() will return 1.1.1601 if the file does not exist)
        string outputPath = path.Replace(_inputDir, _outputDir);
        return File.GetLastWriteTimeUtc(_teosEngine.ContentPath + path) >
               File.GetLastWriteTimeUtc(_teosEngine.BuildPath + outputPath);
    }

    /// <summary>
    /// Build output file(s) from an input file, asyncronously.
    /// </summary>
    /// <param name="path">Relative path to input file</param>
    /// <returns>Async task</returns>
    public async Task Output(string path)
    {
        // output directory
        string outputPath = path.Replace(_inputDir, _outputDir);
            
        // create destination directory if not exists
        Directory.CreateDirectory(Path.GetDirectoryName(_teosEngine.BuildPath + outputPath)!);

        // async copy
        using (var sourceStream = new FileStream(_teosEngine.ContentPath + path, FileMode.Open, FileAccess.Read, FileShare.Read,
                   4096, FileOptions.Asynchronous | FileOptions.SequentialScan))
        {
            using (var destinationStream = new FileStream(_teosEngine.BuildPath + outputPath, FileMode.Create, FileAccess.Write, FileShare.None,
                       4096, FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                await sourceStream.CopyToAsync(destinationStream);
            }
        }
    }

    public override string ToString()
    {
        if (_outputDir == _inputDir)
        {
            return $"CopyFile({(_inputDir.Length > 0 ? _inputDir : "<root>")})";
        }
        else
        {
            return
                $"CopyFile({(_inputDir.Length > 0 ? _inputDir : "<root>")} -> {(_outputDir.Length > 0 ? _outputDir : "<root>")})";
        }
    }
}