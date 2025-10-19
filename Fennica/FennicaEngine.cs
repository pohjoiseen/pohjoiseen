using Teos;

namespace Fennica;

/// <summary>
/// Subclassed version of the TeosEngine for Fennica.
/// </summary>
public class FennicaEngine : TeosEngine
{
    /// <summary>
    /// Engine constructor.  Has to be configured subsequently with AddStaticProcessor() and AddContentType()
    /// </summary>
    /// <param name="projectName">Project/configuration name</param>
    /// <param name="contentPath">Absolute input path</param>
    /// <param name="buildPath">Absolute output path</param>
    /// <param name="withDrafts">Include also draft content?  False = skip automatically on load</param>
    public FennicaEngine(string projectName, string contentPath, string buildPath, bool withDrafts = false)
        : base(projectName, contentPath, buildPath, withDrafts)
    {
    }

    /// <summary>
    /// Loads content.  The one difference with TeosEngine is MakeCopiesOfEnglishContentInFinnish() after LoadAll().
    /// </summary>
    /// <param name="force"></param>
    public override async Task Prepare(bool force = false)
    {
        try
        {
            Logger?.LogInformation("{name} initializing, loading and preparing content for serving/generating...", ProjectName);
            IsLoading = true;
            await LoadAll();
            MakeCopiesOfEnglishContentInFinnish();
            await ProcessStatic(force);
            await BuildRoutes();
            await FormatContent();
            Logger?.LogInformation("{name} initialized, loading and preparing content for serving/generating completed", ProjectName);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Copy all English content also to Finnish version, if the real Finnish version does not exist.
    /// </summary>
    private void MakeCopiesOfEnglishContentInFinnish()
    {
        foreach (var kv in AllContent.ToList())
        {
            if (kv.Value.Item1 is FennicaContent)
            {
                var fennicaContent = (FennicaContent)kv.Value.Item1;
                if (fennicaContent.Language == "en")
                {
                    var finnishKey = kv.Key.Replace(".en.", ".fi.");
                    if (!AllContent.ContainsKey(finnishKey))
                    {
                        var clonedContent = fennicaContent.Clone();
                        clonedContent.Language = "fi";
                        clonedContent.Name = finnishKey;
                        AllContent.Add(finnishKey, (clonedContent, kv.Value.Item2));
                    }
                }
            }
        }
    }
}