using System.Security;
using Fennica3;
using Holvi;
using Microsoft.EntityFrameworkCore;

namespace KoTi.LiveJournal;

/// <summary>
/// Generates HTML for crossposting a post to LiveJournal: post is rendered with Fennica3 ContentFormatter
/// as for the blog, then converted by <see cref="LJHtmlConverter"/>.
/// </summary>
public class LJCrosspostFormatter(HolviDbContext dbContext, ContentFormatter contentFormatter, Helpers helpers)
{
    /// <summary>
    /// LJ HTML for a post.
    /// </summary>
    /// <param name="id">Post ID</param>
    /// <param name="language">Post language, must match</param>
    /// <param name="options">Conversion settings</param>
    /// <returns>HTML, null if post not found</returns>
    public async Task<string?> FormatAsync(int id, string language, LJCrosspostOptions options)
    {
        var post = await dbContext.Posts
            .Include(p => p.TitlePicture)
            .Include(p => p.Book)
            .FirstOrDefaultAsync(p => p.Id == id && p.Language == language);
        if (post is null)
        {
            return null;
        }

        var contentHtml = await contentFormatter.FormatMarkdownAsync(LJHtmlConverter.PrepareMarkdown(post.ContentMD), language);

        // title picture formatted the same way as when shown in text in Fennica3 (see Post.cshtml)
        string? titlePictureHtml = null;
        if (post.TitlePictureId is not null)
        {
            titlePictureHtml = await contentFormatter.FormatHTMLAsync(
                $"<p><img alt=\"{SecurityElement.Escape(post.TitleImageCaption ?? "")}\" src=\"picture:{post.TitlePictureId}\" /></p>",
                language);
        }

        var baseUrl = Fennica3.Fennica3.PublicBase;
        return LJHtmlConverter.Convert(contentHtml, titlePictureHtml, baseUrl, baseUrl + helpers.PostLink(post),
            language, options);
    }
}
