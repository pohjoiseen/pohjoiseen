﻿using System.Globalization;
using System.Text.RegularExpressions;
using Teos;

namespace Fennica2;

/// <summary>
/// Base class for Fennica controllers with some useful functions.
/// </summary>
public abstract class FennicaController : IContentController
{
    protected string ContentPath;
    protected string BuildPath;
    protected IDictionary<string, IStaticProcessor> StaticFiles;
    protected IDictionary<string, (Content, IContentController)> AllContent;
    protected IFormatHTML HTMLFormat;

    /// <inheritdoc cref="IContentController.GetRoutes"/>
    public abstract IList<string> GetRoutes(Content content);

    /// <inheritdoc cref="IContentController.GetURLs"/>
    public virtual IList<string> GetURLs(Content content)
    {
        return GetRoutes(content);
    }

    /// <inheritdoc cref="IContentController.ApplyFormatting"/>
    public virtual Task ApplyFormatting(Content content)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc cref="IContentController.Render"/>
    public virtual Task Render(Content content, GroupCollection routeMatch, HttpResponse response)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc cref="IContentController.SetParameters"/>
    public void SetParameters(string contentPath, string buildPath,
        IDictionary<string, IStaticProcessor> staticFiles,
        IDictionary<string, (Content, IContentController)> allContent,
        IFormatHTML htmlFormat)
    {
        ContentPath = contentPath;
        BuildPath = buildPath;
        StaticFiles = staticFiles;
        AllContent = allContent;
        HTMLFormat = htmlFormat;
    }

    /// <summary>
    /// Sets thread-wide language from a content item, so that localization can be properly used for it.
    /// </summary>
    /// <param name="content">Content item</param>
    protected void SetLanguage(FennicaContent content)
    {
        CultureInfo.CurrentCulture = new CultureInfo(content.Language);
        CultureInfo.CurrentUICulture = new CultureInfo(content.Language);
    }

    /// <summary>
    /// Looks for all known language versions of the content item.
    /// </summary>
    /// <param name="path">Content item path</param>
    /// <returns>Map of language -> path</returns>
    protected IDictionary<string, string> GetLanguageVersionURLs(string path)
    {
        var result = new Dictionary<string, string>();
        // name.ru.article.md -> name, ru, article, md; ru is the part we need to replace
        var splitPath = path.Split('.');
        if (splitPath.Length < 4)
        {
            return result;  // shouldn't happen
        }

        foreach (var lang in Fennica.Languages)
        {
            splitPath[splitPath.Length - 3] = lang;
            var langPath = string.Join('.', splitPath);
            if (AllContent.ContainsKey(langPath))
            {
                result[lang] = AllContent[langPath].Item1.CanonicalURL;
            }
        }

        return result;
    }
}