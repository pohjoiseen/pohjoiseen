using System.Text.RegularExpressions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace Teos;

/// <summary>
/// Processes images with possible resizing, handled by ImageSharp library.
/// </summary>
public class ImageProcessor : IStaticProcessor
{
    private readonly ImageProcessorConfiguration _configuration;

    private ITeosEngine _teosEngine;

    public void SetTeosEngine(ITeosEngine teosEngine)
    {
        _teosEngine = teosEngine;
    }

    /// <summary>
    /// ImageProcessor constructor.
    /// </summary>
    /// <param name="configuration">Full configuration</param>
    public ImageProcessor(ImageProcessorConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Can this processor in its current configuration handle a particular file?
    /// </summary>
    /// <param name="path">Relative filepath to check against</param>
    /// <returns>Yes/No</returns>
    public bool Match(string path)
    {
        // must be in input dir
        if (_configuration.InputDir.Length > 0 && !path.StartsWith(_configuration.InputDir + "/"))
        {
            return false;
        }

        // must not match exclude regexps, if any
        if (_configuration.ExcludeRegexps != null)
        {
            foreach (string excludeRegexp in _configuration.ExcludeRegexps)
            {
                if (Regex.IsMatch(path, excludeRegexp)) { return false; }
            }
        }

        // must be of particular type
        string extension = Path.GetExtension(path);
        if (!_configuration.Types.Contains(extension))
        {
            return false;
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
        // output directory
        string outputPath = path;
        if (_configuration.OutputDir != null)
        {
            outputPath = path.Replace(_configuration.InputDir, _configuration.OutputDir);
        }

        // original mtime
        DateTime inputDateTime = File.GetLastWriteTimeUtc(_teosEngine.ContentPath + path);

        // to be able to determine exact output file names, we need to know the size of the original
        // (because images are not upscaled, and sizes that would be larger than the original are never generated).
        // But this operation needs to be rather quick, so we don't really want to open every original file for this
        // So we use a simple heuristics: the output is stale if either 1) no output files exist at all 2) at least one output
        // file exists and is older than the original.
        // This is not perfect; if the program crashes/is interrupted during image file generation, it might remain with some
        // sizes not generated.  That would require some manual intervention by the user.  But should hopefully be a rare
        // enough case.  In the future we should use a cache for storing file sizes too
        bool anyOutputExists = false;

        // check original
        if (!_configuration.NoOriginal)
        {
            bool outputExists = File.Exists(_teosEngine.BuildPath + outputPath);
            if (outputExists)
            { 
                DateTime outputDateTime = File.GetLastWriteTimeUtc(_teosEngine.BuildPath + outputPath);
                if (inputDateTime > outputDateTime)
                {
                    // exists and stale
                    return true;
                }
            }
            anyOutputExists = outputExists;
        }

        // check resized versions
        if (_configuration.Sizes != null)
        {
            foreach (string size in _configuration.Sizes.Keys)
            {
                string resizedOutputPath = GetPathVersionForSize(outputPath, size);
                bool outputExists = File.Exists(_teosEngine.BuildPath + resizedOutputPath);
                if (outputExists)
                {
                    DateTime outputDateTime = File.GetLastWriteTimeUtc(_teosEngine.BuildPath + resizedOutputPath);
                    if (inputDateTime > outputDateTime)
                    {
                        // exists and stale
                        return true;
                    }
                }
                anyOutputExists |= outputExists;
            }
        }

        return !anyOutputExists;  // none exist -> stale
    }

    /// <summary>
    /// Build output file(s) from an input file, asyncronously.
    /// </summary>
    /// <param name="path">Relative path to input file</param>
    /// <returns>Async task</returns>
    public async Task Output(string path)
    {
        // output directory
        string outputPath = path;
        if (_configuration.OutputDir != null)
        {
            outputPath = path.Replace(_configuration.InputDir, _configuration.OutputDir);
        }

        // create destination directory if not exists
        Directory.CreateDirectory(Path.GetDirectoryName(_teosEngine.BuildPath + outputPath)!);

        // async copy of original, unless explicitly disabled
        if (!_configuration.NoOriginal)
        {
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

        // do we need resized versions?  (well if not, might as well use CopyFileProcessor, but still handle this case)
        if (_configuration.Sizes == null)
        {
            return;
        }

        // load image for resized versions
        using (Image image = await Image.LoadAsync(_teosEngine.ContentPath + path))
        {
            foreach (var size in _configuration.Sizes)
            {
                string sizeSuffix = size.Key;
                int targetSize = size.Value;

                // determine target sizes, always keeping aspect ratio, making the short side fit desired size
                // and skipping if image is already smaller than the desired size.
                // TODO: more options here?
                int width = image.Width, height = image.Height;
                double scale;
                if (width > height)
                {
                    if (height < targetSize)
                    {
                        // too small, don't generate this size
                        continue;
                    }
                    else
                    {
                        scale = targetSize * 1.0 / height;
                    }
                }
                else
                {
                    if (width < targetSize)
                    {
                        // too small, don't generate this size
                        continue;
                    }
                    else
                    {
                        scale = targetSize * 1.0 / width;
                    }
                }
                width = (int) (width * scale);
                height = (int) (height * scale);

                // actually resize and save
                // TODO: make resampler of ImageSharp configurable?  Bicubic is default and should be plenty good enough but still
                // AFAIU these operations take advantage of multiple cores by default
                using (Image resizedImage = image.Clone(x => x.Resize(width, height)))
                {
                    // clear metadata from resized versions
                    resizedImage.Metadata.ExifProfile = null;
                    resizedImage.Metadata.XmpProfile = null;

                    // for JPEGs allow configurable quality
                    // TODO: for other formats?
                    if (path.EndsWith(".jpg") || path.EndsWith(".jpeg"))
                    {
                        await resizedImage.SaveAsync(GetPathVersionForSize(_teosEngine.BuildPath + outputPath, sizeSuffix), new JpegEncoder { Quality = _configuration.Quality });
                    }
                    else
                    {
                        await resizedImage.SaveAsync(GetPathVersionForSize(_teosEngine.BuildPath + outputPath, sizeSuffix));
                    }
                }
            }
        }
    }

    // ("file.png", "1x") -> "file.1x.png"
    private string GetPathVersionForSize(string path, string sizeSuffix)
    {
        int dotIndex = path.LastIndexOf('.');
        return path.Substring(0, dotIndex) + '.' + sizeSuffix + path.Substring(dotIndex);
    }

    public override string ToString()
    {
        // describe our configuration
        string info = string.Join('/', _configuration.Types);
        if (_configuration.Sizes != null)
        {
            info += " " + string.Join('/', _configuration.Sizes.Keys);
        }
        if (_configuration.NoOriginal)
        {
            info += "/NoOriginal";
        }

        if (_configuration.OutputDir == null)
        {
            return $"Image({(_configuration.InputDir.Length > 0 ? _configuration.InputDir.Length : "<root>")}, {info})";
        }
        else
        {
            return
                $"Image({(_configuration.InputDir.Length > 0 ? _configuration.InputDir.Length : "<root>")} -> {(_configuration.OutputDir.Length > 0 ? _configuration.OutputDir.Length : "<root>")}, {info})";
        }
    }
}
