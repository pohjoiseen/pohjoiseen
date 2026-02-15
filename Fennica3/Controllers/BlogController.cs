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
using Microsoft.Extensions.Localization;

namespace Fennica3.Controllers;

public class BlogController(HolviDbContext dbContext, Helpers helpers, ContentFormatter contentFormatter,
                            IStringLocalizer<Fennica3> localizer, IConfiguration configuration) : Controller
{
    [HttpGet("/{language}/")]
    [HttpGet("/{language}/{page:int}/")]
    public async Task<IActionResult> Blog(string language, int page = 1)
    {
        var posts = await GetPostsAsync(language,Fennica3.PostsPerPage, (page - 1) * Fennica3.PostsPerPage);
        if (posts.Count == 0)
        {
            return NotFound();
        }
        
        var totalPages = await CountPagesAsync(language, null);
        var layoutParams = new LayoutParams
        {
            Title = "blog",
            Language = language,
            CanonicalLink = Url.Action("Blog", new { language })!,
            BodyClass = "body-blog",
            BigHeaderPage = "blog",
            RSSLink = Url.Action("RSS", new { language })!
        };

        var introText = (await dbContext.Articles
            .Where(a => a.Name == "_blog-intro" && a.Language == language)
            .FirstOrDefaultAsync())?.ContentMD;
        var extraToCText = (await dbContext.Articles
            .Where(a => a.Name == "_blog-extra-contents" && a.Language == language)
            .FirstOrDefaultAsync())?.ContentMD;
        
        var model = new BlogModel
        {
            Posts = posts,
            LayoutParams = layoutParams,
            Page = page,
            TotalPages = totalPages,
            Pagination = GetPagesForPagination(totalPages, page),
            IntroText = introText,
            ExtraToCText = extraToCText,
        };

        return View("Blog", model);
    }
    
    [HttpGet("/{language}/{bookName}/")]
    [HttpGet("/{language}/{bookName}/{page:int}/")]
    public async Task<IActionResult> Book(string language, string bookName, int page = 1)
    {
        var book = await dbContext.Books
            .Where(b => b.Language == language)
            .Where(b => b.Name == bookName)
            .Where(b => configuration["Fennica3:WithDrafts"] != null || !b.Draft)
            .Include(b => b.TitlePicture)
            .FirstOrDefaultAsync();
        if (book is null)
        {
            return NotFound();
        }
        
        var posts = await GetPostsInBookAsync(language, book.Id, Fennica3.PostsPerPage, (page - 1) * Fennica3.PostsPerPage);
        if (posts.Count == 0)
        {
            return NotFound();
        }
        
        var totalPages = await CountPagesAsync(language, book.Id);
        var layoutParams = new LayoutParams
        {
            Title = book.Title,
            Language = language,
            TitleImage = book.TitlePicture?.Url ?? "",
            CanonicalLink = Url.Action("Book", new { language, bookName })!,
            BodyClass = "body-blog body-book",
        };

        var model = new BlogModel
        {
            Posts = posts,
            Book = book,
            LayoutParams = layoutParams,
            Page = page,
            TotalPages = totalPages,
            Pagination = GetPagesForPagination(totalPages, page),
            IntroText = book.ContentMD,
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

        if (post.BookId != null)
        {
            return RedirectToActionPermanent("PostInBook", new { language, bookName = post.Book!.Name, name = post.Name });
        }
        
        var prevPost = await dbContext.Posts
            .Where(p => p.Language == language)
            .Where(p => configuration["Fennica3:WithDrafts"] != null || !p.Draft)
            .Where(p => p.Date < post.Date || (p.Date == post.Date && p.Name.CompareTo(post.Name) < 0))
            .OrderByDescending(p => p.Date)
            .ThenByDescending(p => p.Name)
            .FirstOrDefaultAsync();
        var nextPost = await dbContext.Posts
            .Where(p => p.Language == language)
            .Where(p => configuration["Fennica3:WithDrafts"] != null || !p.Draft)
            .Where(p => p.Date > post.Date || (p.Date == post.Date && p.Name.CompareTo(post.Name) > 0))
            .OrderBy(p => p.Date)
            .ThenBy(p => p.Name)
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
    
    
    [HttpGet("/{language}/{bookName}/{name}/")]
    public async Task<IActionResult> PostInBook(string language, string bookName, string name)
    {
        var book = await dbContext.Books
            .Where(b => b.Language == language)
            .Where(b => b.Name == bookName)
            .Where(b => configuration["Fennica3:WithDrafts"] != null || !b.Draft)
            .FirstOrDefaultAsync();
        if (book is null)
        {
            return NotFound();
        }

        var post = await dbContext.Posts
            .Include(p => p.TitlePicture)
            .Include(p => p.Book)
            .Where(p => p.Language == language)
            .Where(p => p.BookId == book.Id)
            .Where(p => p.Name == name)
            .Where(p => configuration["Fennica3:WithDrafts"] != null || !p.Draft)
            .FirstOrDefaultAsync();
        if (post == null)
        {
            return NotFound();
        }
        
        var prevPost = await dbContext.Posts
            .Where(p => p.Language == language)
            .Where(p => p.BookId == book.Id)
            .Where(p => configuration["Fennica3:WithDrafts"] != null || !p.Draft)
            .Where(p => p.Order < post.Order || (p.Order == post.Order && p.Date.CompareTo(post.Date) < 0))
            .OrderByDescending(p => p.Order)
            .ThenByDescending(p => p.Date)
            .FirstOrDefaultAsync();
        var nextPost = await dbContext.Posts
            .Where(p => p.Language == language)
            .Where(p => p.BookId == book.Id)
            .Where(p => configuration["Fennica3:WithDrafts"] != null || !p.Draft)
            .Where(p => p.Order > post.Order || (p.Order == post.Order && p.Date.CompareTo(post.Date) > 0))
            .OrderBy(p => p.Order)
            .ThenBy(p => p.Date)
            .FirstOrDefaultAsync();

        var layoutParams = new LayoutParams
        {
            Title = post.Title,
            Language = language,
            CanonicalLink = helpers.PostLink(post),
            BodyClass = "body-post",
            TitleImage = post.TitlePicture?.Url ?? "",
            UpPath = Url.Action("Book", new { language, bookName })!,
            UpTitle = book.Title,
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
            .Include(p => p.Book)
            .Include(p => p.TitlePicture)
            .FirstOrDefaultAsync(p => p.Language == language && p.Date == date &&
                                      p.Name == name && (configuration["Fennica3:WithDrafts"] != null || !p.Draft));
        return post;
    }
    
    private async Task<IList<Post>> GetPostsAsync(string language, int limit, int offset = 0)
    {
        return await dbContext.Posts
            .Where(p => p.Language == language)
            .Where(p => p.BookId == null)
            .Where(p => configuration["Fennica3:WithDrafts"] != null || !p.Draft)
            .OrderByDescending(p => p.Date)
            .ThenByDescending(p => p.Name)
            .Include(p => p.Book)
            .Include(p => p.TitlePicture)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();
    }
    
    private async Task<IList<Post>> GetPostsInBookAsync(string language, int bookId, int limit, int offset = 0)
    {
        return await dbContext.Posts
            .Where(p => p.Language == language)
            .Where(p => p.BookId == bookId)
            .Where(p => configuration["Fennica3:WithDrafts"] != null || !p.Draft)
            .OrderBy(p => p.Order)
            .ThenBy(p => p.Date)
            .ThenBy(p => p.Name)
            .Include(p => p.Book)
            .Include(p => p.TitlePicture)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();
    }

    private async Task<int> CountPagesAsync(string language, int? bookId)
    {
        int postsCount = await dbContext.Posts
            .Where(p => p.Language == language)
            .Where(p => p.BookId == bookId)
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