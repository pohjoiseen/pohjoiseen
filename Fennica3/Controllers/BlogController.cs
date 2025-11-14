using System.ServiceModel.Syndication;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using System.Xml;
using Fennica3.ResponseModels;
using Fennica3.ViewModels;
using Holvi;
using Holvi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.Json;
using Microsoft.Extensions.Localization;

namespace Fennica3.Controllers;

public class BlogController(HolviDbContext dbContext, Helpers helpers, ContentFormatter contentFormatter,
                            IStringLocalizer<Fennica3> localizer, IConfiguration configuration) : Controller
{
    [HttpGet("/{language}/")]
    [HttpGet("/{language}/{page:int}/")]
    public async Task<IActionResult> Blog(string language, int page = 1)
    {
        var posts = await GetPostsAsync(language, Fennica3.PostsPerPage, (page - 1) * Fennica3.PostsPerPage);
        if (posts.Count == 0)
        {
            return NotFound();
        }
        
        var totalPages = await CountPagesAsync(language);
        var layoutParams = new LayoutParams
        {
            Title = "blog",
            Language = language,
            CanonicalLink = Url.Action("Blog", new { language })!,
            BodyClass = "body-blog",
            BigHeaderPage = "blog",
            RSSLink = Url.Action("RSS", new { language })!
        };

        var mapAsideText = (await dbContext.Articles
            .Where(a => a.Name == "_blog-map-aside" && a.Language == language)
            .FirstOrDefaultAsync())?.ContentMD;
        var introText = (await dbContext.Articles
            .Where(a => a.Name == "_blog-intro" && a.Language == language)
            .FirstOrDefaultAsync())?.ContentMD;
        
        var model = new BlogModel
        {
            Posts = posts,
            LayoutParams = layoutParams,
            Page = page,
            TotalPages = totalPages,
            Pagination = GetPagesForPagination(totalPages, page),
            MapAsideText = mapAsideText,
            IntroText = introText
        };

        return View("Blog", model);
    }

    [HttpGet("/{language}/{year:int}/{month:int}/{day:int}/{name}/")]
    public async Task<IActionResult> Post(string language, int year, int month, int day, string name)
    {
        var post = await GetPostAsync(language, year, month, day, name);
        if (post == null)
        {
            return NotFound();
        }
        
        var prevPost = await dbContext.Posts
            .Where(p => p.Language == language)
            .Where(p => configuration["Fennica3:WithDrafts"] != null || !p.Draft)
            .Where(p => p.Date < post.Date || (p.Date == post.Date && p.Name.CompareTo(post.Name) < 0))
            .OrderByDescending(p => p.Date)
            .ThenByDescending(p => p.Name)
            .Include(p => p.TitlePicture)
            .FirstOrDefaultAsync();
        var nextPost = await dbContext.Posts
            .Where(p => p.Language == language)
            .Where(p => configuration["Fennica3:WithDrafts"] != null || !p.Draft)
            .Where(p => p.Date > post.Date || (p.Date == post.Date && p.Name.CompareTo(post.Name) > 0))
            .OrderBy(p => p.Date)
            .ThenBy(p => p.Name)
            .Include(p => p.TitlePicture)
            .FirstOrDefaultAsync();

        var layoutParams = new LayoutParams
        {
            Title = post.Title,
            Language = language,
            CanonicalLink = helpers.PostLink(post),
            BodyClass = "body-post",
            TitleImage = post.TitlePicture?.Url ?? "",
            PrevPath = prevPost == null ? "" : helpers.PostLink(prevPost),
            PrevTitle = prevPost == null ? "" : prevPost.Title,
            NextPath = nextPost == null ? "" : helpers.PostLink(nextPost),
            NextTitle = nextPost == null ? "" : nextPost.Title
        };

        var model = new PostModel
        {
            Post = post,
            PrevPost = prevPost,
            NextPost = nextPost,
            LayoutParams = layoutParams
        };

        return View("Post", model);
    }

    [HttpGet("/{language}/{year:int}/{month:int}/{day:int}/{name}.json")]
    [Produces("application/json")]
    public async Task<ActionResult> PostJson(string language, int year, int month, int day, string name)
    {
        var post = await GetPostAsync(language, year, month, day, name);
        if (post == null)
        {
            return NotFound();
        }

        var postModel = new PostDefinitionForMap
        {
            Id = post.Id,
            Mini = post.Mini,
            Title = post.Title,
            TitleImage = post.TitlePicture?.DetailsUrl ?? "",
            Description = await contentFormatter.FormatMarkdownAsync(post.Description, true),
            Geo = await Task.WhenAll(helpers.ResolveGeos(post).Select(async geo =>
            {
                if (geo.Description == null)
                {
                    return geo;
                }
                var description = await contentFormatter.FormatMarkdownAsync(geo.Description, true);
                return geo with { Description = description };
            }))
        };

        return Json(postModel, new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic),
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new Helpers.DateOnlyJsonConverter() },
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            IncludeFields = true
        });
    }

    [HttpGet("/{language}/rss.xml")]
    public async Task RSS(string language)
    {
        // take the first page of posts
        var posts = await GetPostsAsync(language, Fennica3.PostsPerPage);

        // init feed
        var feed = new SyndicationFeed(Fennica3.MainTitle, localizer["rss-description"],
            new Uri($"{Fennica3.PublicBase}/{language}/"));
        feed.Generator = "Fennica3";
        feed.Language = language;
        feed.Copyright = new TextSyndicationContent(Fennica3.Copyright + " " + Fennica3.Author);
        feed.LastUpdatedTime = DateTimeOffset.Now;

        // feed entries
        var items = new List<SyndicationItem>();
        foreach (var post in posts)
        {
            var item = new SyndicationItem(post.Title, await contentFormatter.FormatMarkdownAsync(post.Description, true),
                new Uri(Fennica3.PublicBase + helpers.PostLink(post)),
                Fennica3.PublicBase + helpers.PostLink(post),
                new DateTimeOffset(post.Date.ToDateTime(TimeOnly.MinValue)));
            item.PublishDate = item.LastUpdatedTime;
            if (post.TitlePicture != null)
            {
                var link = SyndicationLink.CreateMediaEnclosureLink(new Uri(post.TitlePicture.Url),
                    "image/jpeg", 0);
                item.Links.Add(link);
            }

            items.Add(item);
        }

        feed.Items = items;

        // write out
        Response.ContentType = "application/rss+xml; charset=utf-8";
        await using var xmlWriter = XmlWriter.Create(Response.Body, new XmlWriterSettings { Encoding = Encoding.UTF8, Async = true });
        var rssFormatter = new Rss20FeedFormatter(feed);
        rssFormatter.WriteTo(xmlWriter);
        await xmlWriter.FlushAsync();
    }

    private async Task<Post?> GetPostAsync(string language, int year, int month, int day, string name)
    {
        DateOnly date;
        // swallow improperly parsed date, this usually means just wrong URL
        try
        {
            date = new DateOnly(year, month, day);
        }
        catch (Exception)
        {
            return null;
        }

        var post = await dbContext.Posts
            .Include(p => p.TitlePicture)
            .FirstOrDefaultAsync(p => p.Language == language && p.Date == date &&
                                      p.Name == name && (configuration["Fennica3:WithDrafts"] != null || !p.Draft));
        return post;
    }
    
    private async Task<IList<Post>> GetPostsAsync(string language, int limit, int offset = 0)
    {
        return await dbContext.Posts
            .Where(p => p.Language == language)
            .Where(p => configuration["Fennica3:WithDrafts"] != null || !p.Draft)
            .OrderByDescending(p => p.Date)
            .ThenByDescending(p => p.Name)
            .Include(p => p.TitlePicture)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();
    }

    private async Task<int> CountPagesAsync(string language)
    {
        int postsCount = await dbContext.Posts
            .Where(p => p.Language == language)
            .CountAsync(p => configuration["Fennica3:WithDrafts"] != null || !p.Draft);
        int num = postsCount / Fennica3.PostsPerPage;
        if (postsCount % Fennica3.PostsPerPage != 0)
        {
            num++;
        }
        return num;
    }

    /// <summary>
    /// Get a list of page links to display at the bottom, for given number of pages and current pages.
    /// 0 in the list = display "..."
    /// </summary>
    /// <param name="numPages">Total number of pages</param>
    /// <param name="thisPage">Number of this page</param>
    /// <returns>Page numbers to show links for</returns>
    private IList<int> GetPagesForPagination(int numPages, int thisPage)
    {
        var pages = new List<int>();
        if (numPages < 11)
        {
            pages.AddRange(Enumerable.Range(1, numPages));
        }
        else if (thisPage <= 6)
        {
            pages.AddRange(Enumerable.Range(1, thisPage + 2));
            pages.Add(0);
            pages.AddRange(Enumerable.Range(numPages - 2, 3));
        }
        else if (numPages - thisPage <= 6)
        {
            pages.AddRange(Enumerable.Range(1, 3));
            pages.Add(0);
            pages.AddRange(Enumerable.Range(thisPage - 2, numPages - thisPage + 3));
        }
        else
        {
            pages.AddRange(Enumerable.Range(1, 3));
            pages.Add(0);
            pages.AddRange(Enumerable.Range(thisPage - 2, 5));
            pages.Add(0);
            pages.AddRange(Enumerable.Range(numPages - 2, 3));
        }

        return pages;
    }
}