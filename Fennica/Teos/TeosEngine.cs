using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Teos;

/// <summary>
/// The central class of the static site generator.
/// </summary>
public class TeosEngine : ITeosEngine
{
    //// Configuration

    /// <summary>
    /// Project/configuration name.
    /// </summary>
    private readonly string _projectName;

    public string ProjectName => _projectName;
        
    /// <summary>
    /// Where the content is scanned for.
    /// </summary>
    private readonly string _contentPath;
    public string ContentPath => _contentPath;
    
    /// <summary>
    /// Where the content is written out to.
    /// </summary>
    private readonly string _buildPath;
    public string BuildPath => _buildPath;
    
    /// <summary>
    /// Filter out all draft content.
    /// </summary>
    private readonly bool _withDrafts;
    public bool WithDrafts => _withDrafts;

    /// <summary>
    /// List of all processors defined.
    /// </summary>
    private IList<IStaticProcessor> _staticProcessors = new List<IStaticProcessor>();

    /// <summary>
    /// Map of all content types defined, from regexp matching the filename to loaders, controllers and backing types.
    /// </summary>
    private IDictionary<string, (IContentLoader, IContentController, Type)> _contentTypes =
        new Dictionary<string, (IContentLoader, IContentController, Type)>();

    /// <summary>
    /// List of all formatters to postprocess HTML.
    /// </summary>
    private IList<IContentFormatter> _formatters = new List<IContentFormatter>();

    /// <summary>
    /// Optional logger.
    /// </summary>
    private ILogger _logger;
    protected ILogger Logger => _logger;
        
    //// Loaded state
        
    /// <summary>
    /// Map of all static files to their processors
    /// </summary>
    private Dictionary<string, IStaticProcessor> _staticFiles = new();

    public IDictionary<string, IStaticProcessor> StaticFiles => _staticFiles;
        
    /// <summary>
    /// Map of all dynamic content files to loaded object and controller.
    /// </summary>
    private IDictionary<string, (Content, IContentController)> _allContent =
        new Dictionary<string, (Content, IContentController)>();

    public IDictionary<string, (Content, IContentController)> AllContent => _allContent;

    /// <summary>
    /// Map of URLs to content items.
    /// </summary>
    private IDictionary<string, string> _routeMap =
        new Dictionary<string, string>();

    //// Other variables

    /// <summary>
    /// Whether we are in the loading state.
    /// </summary>
    protected bool IsLoading;

    /// <summary>
    /// FS watcher for reloading content.
    /// </summary>
    private FileSystemWatcher _watcher;

    private object _staticFilesLock = new object(), _contentLock = new object();
        
    /// <summary>
    /// Engine constructor.  Has to be configured subsequently with AddStaticProcessor() and AddContentType()
    /// </summary>
    /// <param name="projectName">Project/configuration name</param>
    /// <param name="contentPath">Absolute input path</param>
    /// <param name="buildPath">Absolute output path</param>
    /// <param name="withDrafts">Include also draft content?  False = skip automatically on load</param>
    public TeosEngine(string projectName, string contentPath, string buildPath, bool withDrafts = false)
    {
        _projectName = projectName;
        // TODO: check that neither path is a descendant of another, just for safety
        _contentPath = contentPath.TrimEnd('/') + '/';
        _buildPath = buildPath.TrimEnd('/') + '/';
        _withDrafts = withDrafts;
    }
    
    /// <summary>
    /// Adds a processor to the configuration.  Processors will try to match files in the order they
    /// were added with AddProcessor().
    /// </summary>
    /// <param name="processor">Static processor</param>
    /// <returns>Self, for chaining</returns>
    public TeosEngine AddStaticProcessor(IStaticProcessor processor)
    {
        processor.SetTeosEngine(this);
        _staticProcessors.Add(processor);
        return this;
    }

    /// <summary>
    /// Adds a content type to the configuration.
    /// </summary>
    /// <typeparam name="T">Backing type</typeparam>
    /// <param name="pathRegexp">Regexp which must match the content file path.  Regexps
    ///     of different content types will be matched in the order these types are added</param>
    /// <param name="loader">Class to load content file and transform into object of backing type</param>
    /// <param name="controller">Class to render content</param>
    /// <returns>Self, for chaining</returns>
    public TeosEngine AddContentType<T>
    (string pathRegexp,
        IContentLoader loader,
        IContentController controller)
        where T : Content
    {
        loader.SetTeosEngine(this);
        controller.SetTeosEngine(this);
        _contentTypes.Add(pathRegexp, (loader, controller, typeof(T)));
        return this;
    }

    /// <summary>
    /// Adds a formatter, which will be used to postprocess all HTML.
    /// </summary>
    /// <param name="formatter">Formatter</param>
    /// <returns>Self, for chaining</returns>
    public TeosEngine AddHTMLFormatter(IContentFormatter formatter)
    {
        formatter.SetTeosEngine(this);
        _formatters.Add(formatter);
        return this;
    }

    /// <summary>
    /// Adds a logger.  This is optional.
    /// </summary>
    /// <param name="logger">Logger</param>
    /// <returns>Self, for chaining</returns>
    public TeosEngine SetLogger(ILogger logger)
    {
        _logger = logger;
        return this;
    }

    private enum LoadResult
    {
        LoadedStatic,
        LoadedContent,
        NotMatched,
        DraftSkipped,
        Error
    }

    private string CanonicalizeContentPath(string rawPath)
    {
        return rawPath.Substring(_contentPath.Length).Replace(Path.DirectorySeparatorChar, '/');
    }

    /// <summary>
    /// Loads a single file.
    /// Content files are actually loaded, static files are only recorded in static file map.
    /// </summary>
    /// <param name="path">File to load, relative to content path</param>
    /// <returns>One of possible outcomes, see LoadResult enum</returns>
    private async Task<LoadResult> LoadFile(string path)
    {
        // try to match with every static processor in order
        foreach (IStaticProcessor processor in _staticProcessors)
        {
            if (processor.Match(path))
            {
                lock (_staticFilesLock)
                { 
                    _staticFiles[path] = processor;
                }
                //_logger?.LogTrace("Found static: {file} -> {processor}", path, processor.ToString());
                return LoadResult.LoadedStatic;
            }
        }

        // try to match with content types
        foreach (var kv in _contentTypes)
        {
            if (Regex.IsMatch(path, kv.Key))
            {
                var loader = kv.Value.Item1;
                var controller = kv.Value.Item2;
                var type = kv.Value.Item3;

                // load if matched
                try
                {
                    // read in content
                    FileStream fs = File.OpenRead(_contentPath + path);
                    Content content = await loader.LoadContent(path, fs, type);
                    fs.Close();

                    // skip drafts, if requested
                    if (!_withDrafts && content.Draft)
                    {
                        return LoadResult.DraftSkipped;
                    }

                    // store
                    lock (_contentLock)
                    {
                        _allContent[path] = (content, controller);
                    }
                    //_logger?.LogTrace("Loaded content: {file} -> {type}", path, content.ToString());
                    return LoadResult.LoadedContent;
                }
                catch (Exception e)
                {
                    _logger?.LogError("Error loading content file {file}: {message}", path, e.ToString());
                    return LoadResult.Error;
                }
            }
        }

        return LoadResult.NotMatched;
    }

    /// <summary>
    /// Scans/rescans the input dir for all files that the engine in its current configuration can process.
    /// </summary>
    /// <returns>Async task</returns>
    public async Task LoadAll()
    {
        _staticFiles.Clear();
        _allContent.Clear();
        _logger?.LogInformation("Running content full scan...");

        int notMatched = 0, errors = 0, skipped = 0;

        await Parallel.ForEachAsync(Directory.EnumerateFiles(_contentPath, "*.*", SearchOption.AllDirectories), async (rawPath, ct) =>
        {
            // ignore all git stuff
            if (rawPath.Contains(".git"))
            {
                return;
            }
            
            LoadResult result = await LoadFile(CanonicalizeContentPath(rawPath));
            switch (result)
            {
                case LoadResult.DraftSkipped:
                    Interlocked.Increment(ref skipped);
                    break;
                    
                case LoadResult.Error:
                    Interlocked.Increment(ref errors);
                    break;
                    
                case LoadResult.NotMatched:
                    Interlocked.Increment(ref notMatched);
                    break;
            }
        });

        _logger?.LogInformation("Running content full scan finished, {static} static file(s) added, {content} content file(s) loaded, " +
                                "{notMatched} unrecognized file(s), {errors} error(s){drafts}",
            _staticFiles.Count, _allContent.Count, notMatched, errors, _withDrafts ? "" : $", {skipped} draft(s) skipped");
    }

    /// <summary>
    /// Generates all routes for the website.
    /// </summary>
    /// <returns>Async task</returns>
    public async Task BuildRoutes()
    {
        _logger?.LogInformation("Generating routes...");
        _routeMap.Clear();
        object routeLock = new object();
        await Parallel.ForEachAsync(_allContent, (kv, ct) =>
        {
            var path = kv.Key;
            var content = kv.Value.Item1;
            var controller = kv.Value.Item2;
            foreach (string pattern in controller.GetRoutes(content))
            {
                lock (routeLock)
                {
                    _routeMap[pattern] = path;
                }
                //_logger?.LogTrace("{route} -> {file}", pattern, path);
            }

            return ValueTask.CompletedTask;
        });
        _logger?.LogInformation("Generating routes finished, {routes} route(s) added", _routeMap.Count);
    }

    /// <summary>
    /// Applies formatting to loaded content.  This should happen when content is already loaded.
    /// </summary>
    /// <returns>Async task</returns>
    public async Task FormatContent()
    {
        _logger?.LogInformation("Formatting HTML content...");
        int success = 0, errors = 0;
        await Parallel.ForEachAsync(_allContent, async (kv, ct) =>
        {
            try
            {
                var content = kv.Value.Item1;
                var controller = kv.Value.Item2;
                await controller.ApplyFormatting(content);
                content.HTML = await FormatHTML(content.HTML, kv.Key);
                Interlocked.Increment(ref success);
            }
            catch (Exception e)
            {
                _logger?.LogError("Error formatting content file {file}: {message}", kv.Key, e.ToString());
                Interlocked.Increment(ref errors);
            }
        });
        _logger?.LogInformation("Formatting HTML content finished, {success} formatted, {errors} error(s)", success, errors);
    }

    /// <summary>
    /// Process and outputs all static files from _contentFiles using the current processor configuration.
    /// </summary>
    /// <param name="force">Reprocess even when output file(s) already exist</param>
    /// <returns>Async task</returns>
    public async Task ProcessStatic(bool force = false)
    {
        // filter out static files to process
        _logger?.LogInformation("Looking for out of date static files...");
        Dictionary<string, IStaticProcessor> staticFiles = _staticFiles
            .Where(cf => force || cf.Value.IsOutputStale(cf.Key))
            .ToDictionary(cf => cf.Key, cf => cf.Value);

        if (staticFiles.Count == 0)
        {
            _logger?.LogInformation("No out of date static files found");
            return;
        }
        _logger?.LogInformation("Processing {count} out of date static file(s) found (async)...", staticFiles.Count);

        // process everything in parallel
        // (do we actually need Parallel and async at the same time?
        // At least seems to do what I want anyway, saturates all cores and processes 13 GB of JPEGs in a bit over 3.5 min)
        int success = 0, errors = 0;
        await Parallel.ForEachAsync(staticFiles, async (cf, ct) =>
        {
            string path = cf.Key;
            IStaticProcessor processor = cf.Value;

            try
            {
                // debug, not trace here, this is typically a somewhat lengthy operation
                _logger?.LogDebug("Processing static file: {file} -> {processor}", path, processor.ToString());
                // do the thing
                await processor.Output(path);
                Interlocked.Increment(ref success);
            }
            catch (Exception e)
            {
                _logger?.LogError("Error processing static file {file} with {processor}: {message}", path, processor.ToString(), e.ToString());
                Interlocked.Increment(ref errors);
            }
        });

        _logger?.LogInformation("Processing {count} out of date static file(s) finished, {success} processed, {errors} failed",
            staticFiles.Count, success, errors);
    }

    /// <summary>
    /// Runs all the initial tasks (scan for files, process static files, load dynamic content).
    /// </summary>
    /// <param name="force">Reprocess static files even when output file(s) already exist</param>
    /// <returns>Async task</returns>
    public virtual async Task Prepare(bool force = false)
    {
        try
        {
            _logger?.LogInformation("{name} initializing, loading and preparing content for serving/generating...", _projectName);
            IsLoading = true;
            await LoadAll();
            await ProcessStatic(force);
            await BuildRoutes();
            await FormatContent();
            _logger?.LogInformation("{name} initialized, loading and preparing content for serving/generating completed", _projectName);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Tries to render a content page under given URL.
    /// </summary>
    /// <param name="url">Relative URL to render</param>
    /// <param name="response">Response to render to</param>
    /// <returns>Whether anything was handled and rendered</returns>
    public async Task<bool> TryRender(string url, HttpResponse response)
    {
        // Until loading is complete, show a stub page that reloads itself in a second 
        if (IsLoading)
        {
            response.ContentType = "text/html; charset=utf-8";
            await response.WriteAsync("<html><head><style>html { width: 100%; height: 100% } body { width: 100%; height: 100%; margin: 0; } " +
                                      "h2 { width: auto; line-height: 100vh; font-family: sans-serif; text-align: center; font-size: 56px; margin: 0; }</style></head>" +
                                      "<body><h2>Loading project " + _projectName + ", please wait...</h2><script>setTimeout(function () { location.reload(); }, 1000)</script></body></html>");
            return true;
        }

        foreach (var kv in _routeMap)
        {
            var result = Regex.Match(url, "^" + kv.Key + "$");
            if (result.Success)
            {
                var content = _allContent[kv.Value].Item1;
                var controller = _allContent[kv.Value].Item2;
                await controller.Render(content, result.Groups, response);
                return true;
            }
        }
        return false;
    }
        
    /// <summary>
    /// Start watching the content directory for changes.
    /// TODO: no way to stop.
    /// </summary>
    public void Watch()
    {
        _watcher = new FileSystemWatcher(_contentPath);
        _watcher.Filter = "*.*";
        _watcher.IncludeSubdirectories = true;
        _watcher.Created += OnWatchChanged;
        _watcher.Changed += OnWatchChanged;
        _watcher.Deleted += OnWatchDeleted;
        _watcher.Renamed += OnWatchRenamed;
        _watcher.Error += OnWatchError;
        _watcher.EnableRaisingEvents = true;
    }

    /// <summary>
    /// Handles added/updated files.  As far as we are concerned these are mostly the same operations.
    ///
    /// Adding or modifying content or static files potentially means that other content might need to be reformatted.
    /// We have no way of tracking that and so, we don't do it.
    /// Watching is meant for local development; if something get out of date, the user can just reload everything.
    ///
    /// TODO: do we need locks here?  Is this multithreaded?
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void OnWatchChanged(object sender, FileSystemEventArgs e)
    {
        // wait 0.2s below doing everything so that we do not end up trying to read a temporary file
        // that is immediately deleted
        await Task.Delay(200);
        if (!File.Exists(e.FullPath))
        {
            return;  // might no longer exist; in practice saving files tends to create temporary files
        }

        if (e.FullPath.Contains(".git") || e.FullPath.EndsWith("~"))
        {
            return;  // ignore git stuff, temporary backup files
        }

        if (File.GetAttributes(e.FullPath).HasFlag(FileAttributes.Directory))
        {
            return;  // skip dirs
        }
            
        string path = CanonicalizeContentPath(e.FullPath);
        _logger?.LogInformation("Added/changed file: {path}", path);
        LoadResult result = await LoadFile(path);
        switch (result)
        {
            case LoadResult.LoadedStatic:
                // static file needs to be [re]processed right away
                var processor = _staticFiles[path];
                _logger?.LogInformation("Processing static file with {processor}", processor.ToString());
                try
                {
                    await _staticFiles[path].Output(path);
                }
                catch (Exception ex)
                {
                    _logger?.LogError("Error processing static file {file} with {processor}: {message}", path, processor.ToString(), ex.ToString());
                }

                break;
                
            case LoadResult.LoadedContent:
                // content needs to be reformatted and routes updated
                var loaded = _allContent[path];
                var content = loaded.Item1;
                var controller = loaded.Item2;
                _logger?.LogInformation("Loaded content file {content}, updating routes and formatting", content.ToString());
                try
                {
                    lock (_routeMap)
                    {
                        // remove old routes associated with this item
                        var routesToRemove = _routeMap.Where(kv => kv.Value == path);
                        foreach (var route in routesToRemove)
                        {
                            _routeMap.Remove(route.Key);
                        }

                        // add routes
                        foreach (var route in controller.GetRoutes(content))
                        {
                            _routeMap[route] = path;
                        }
                    }
                        
                    // format
                    await controller.ApplyFormatting(content);
                    content.HTML = await FormatHTML(content.HTML, path);
                }
                catch (Exception ex)
                {
                    _logger?.LogError("Error processing content file {file}: {message}", path, ex.ToString());
                }

                break;
                    
            case LoadResult.DraftSkipped:
                _logger?.LogInformation("Draft content file, ignoring");
                _staticFiles.Remove(path);
                _allContent.Remove(path);
                break;
                
            case LoadResult.NotMatched:
                _logger?.LogInformation("File not recognized, ignoring");
                _staticFiles.Remove(path);
                _allContent.Remove(path);
                break;
                
            case LoadResult.Error:
                // not doing anything else
                _logger?.LogWarning("File loading error, content in memory unchanged");
                break;
        }
    }

    /// <summary>
    /// Handles deleted files.  We remove those from our in-memory state but don't really do anything else.
    /// In particular output files for static content are not removed.
    ///
    /// Deleting content or static files potentially means that other content might need to be reformatted.
    /// We have no way of tracking that and so, we don't do it.
    /// Watching is meant for local development; if something get out of date, the user can just reload everything.
    ///
    /// TODO: do we need locks here?  Is this multithreaded?
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void OnWatchDeleted(object sender, FileSystemEventArgs e)
    {
        // wait 0.2s below doing everything so that we do not end up trying to react to a temporary file
        // that is immediately recreated
        await Task.Delay(200);
        if (File.Exists(e.FullPath))
        {
            return;  // actually still exists, whatever
        }

        if (e.FullPath.Contains(".git") || e.FullPath.EndsWith("~"))
        {
            return;  // ignore git stuff, temporary backup files
        }

        string path = CanonicalizeContentPath(e.FullPath);
        _logger?.LogInformation("Deleted file: {path}", path);

        if (_staticFiles.ContainsKey(path))
        {
            // TODO: should remove output files
            _logger?.LogInformation("Static file, not removing output files");
            _staticFiles.Remove(path);
        }
        else if (_allContent.ContainsKey(path))
        {
            var loaded = _allContent[path];
            var content = loaded.Item1;
            _logger?.LogInformation("Removed content file {content}, updating routes", content.ToString());
            lock (_routeMap)
            {
                // remove old routes associated with this item
                var routesToRemove = _routeMap.Where(kv => kv.Value == path);
                foreach (var route in routesToRemove)
                {
                    _routeMap.Remove(route.Key);
                }
            }

            _allContent.Remove(path);
        }
        else
        {
            _logger?.LogInformation("Unknown file, ignoring");
        }
    }

    /// <summary>
    /// Handles renamed files.  For our purposes rename can be handled as delete+create.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnWatchRenamed(object sender, RenamedEventArgs e)
    {
        var deletedArgs =
            new FileSystemEventArgs(WatcherChangeTypes.Deleted, Path.GetDirectoryName(e.OldFullPath), Path.GetFileName(e.OldName));
        OnWatchDeleted(sender, deletedArgs);
        OnWatchChanged(sender, e);
    }

    /// <summary>
    /// Handle watch error.  According to .NET docs this can be raised when too many events are raised
    /// too quickly.  We try to recover by stopping watcher, making a full reload and starting watch again.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void OnWatchError(object sender, ErrorEventArgs e)
    {
        _logger?.LogWarning("Content directory watch error: {ex}", e.GetException());
        _logger?.LogInformation("Closing watcher and reloading content");
        _watcher?.Dispose();
        _watcher = null;
        await Prepare();
        Watch();
    }

    /// <summary>
    /// Generate and write out to filesystem all content 
    /// </summary>
    /// <returns>Whether we had any errors</returns>
    public async Task<bool> Generate()
    {
        _logger?.LogInformation("Generating content...");
        int errors = 0;
        await Parallel.ForEachAsync(_allContent, async (kv, ct) =>
        {
            var content = kv.Value.Item1;
            var controller = kv.Value.Item2;
            try
            {
                foreach (var url in controller.GetURLs(content))
                {
                    //_logger?.LogTrace("  {url} -> {content}", url, content.ToString());
                    var path = _buildPath + url;
                    if (url[url.Length - 1] == '/')
                    {
                        path += "index.html";
                    }
                    Directory.CreateDirectory(Path.GetDirectoryName(path));

                    // dummy HttpContext with a dummy HttpResponse, render to a memory stream
                    HttpContext httpContext = new DefaultHttpContext();
                    httpContext.Response.Body = new MemoryStream();
                    bool rendered = await TryRender(url, httpContext.Response);
                    if (!rendered)
                    {
                        throw new Exception(
                            $"URL {url} received from controller for content {content} but not handled, should not happen");
                    }
                    
                    // write out to filesystem whatever has been rendered
                    // we used to do "httpContext.Response.Body = File.OpenWrite(path);" directly,
                    // but for some reason it eventually started to sometimes render corrupted files, with garbage
                    // in the end, seemingly last parts of the HTML repeating randomly a few times, in particular
                    // for Fennica blog content types for pages >1.  Difficult to say why but introducing
                    // this bit of indirection seems to solve the issue, perhaps the renderer wants to be able to
                    // massage data a bit or something
                    httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
                    var reader = new StreamReader(httpContext.Response.Body);
                    string result = reader.ReadToEnd();
                    File.WriteAllText(path, result);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError("Error generating content for {file}: {exception}", kv.Key, ex.ToString());
                Interlocked.Increment(ref errors);
            }
        });
        if (errors > 0)
        {
            _logger?.LogWarning("Generation finished with {error} error(s)", errors);
            return false;
        }
        _logger?.LogInformation("Generation finished!");
        return true;
    }

    /// <summary>
    /// Runs the formatter chain on a fragment of HTML.
    /// </summary>
    /// <param name="html">HTML to postprocess</param>
    /// <param name="path">Path to original content item for which HTML is being formatted</param>
    /// <returns>Postprocessed HTML, async</returns>
    public async Task<string> FormatHTML(string html, string path)
    {
        foreach (IContentFormatter formatter in _formatters)
        {
            html = await formatter.FormatHTML(html, path);
        }
        return html;
    }

    /// <summary>
    /// Resolves relative path to "target" content or static file, from "source" file.  
    /// </summary>
    /// <param name="target">Target content or static file</param>
    /// <param name="source">Source content file</param>
    /// <returns></returns>
    public string ResolvePath(string target, string source)
    {
        // only handle relative paths
        if (target.StartsWith("."))
        {
            // directory name of the source file, if available.  Ensure also '/' directory separator
            var basepath = Path.GetDirectoryName(source) == null ? "" : Path.GetDirectoryName(source)!.Replace('\\', '/');

            // resolve and again ensure '/' directory separator
            if (basepath.Length > 0)
            {
                // TODO: this is really ridiculous, is there no other way...
                return Path.GetFullPath(Path.Combine(basepath, target))
                    .Replace(Directory.GetCurrentDirectory(), "").Replace('\\', '/').Substring(1);
            }

            // we're already in the base dir
            if (target.StartsWith("./"))
            {
                return target.Substring(2);
            }
            
            throw new Exception("Could not resolve path " + target + ", base file already in root directory");
        }

        return target;
    }
}
