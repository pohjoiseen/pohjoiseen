using System.ServiceModel.Syndication;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using Microsoft.Extensions.Localization;
using Razor.Templating.Core;
using Teos;

namespace Fennica2;

/// <summary>
/// Controller for Blog content.
/// </summary>
public class BlogController : FennicaController
{
    private IStringLocalizer<FennicaProject> _localizer;
        
    public BlogController(IStringLocalizer<FennicaProject> localizer)
    {
        _localizer = localizer;
    }
        
    /// <summary>
    /// Get all posts in this blog (matching language, sort by date descending and then title)
    /// </summary>
    /// <param name="blog">Blog</param>
    /// <returns>Posts</returns>
    private List<Post> GetPosts(Blog blog)
    {
        return (from p in TeosEngine.AllContent
            where p.Value.Item1 is Post
                  && ((Post)p.Value.Item1).Language == blog.Language
            orderby ((Post)p.Value.Item1).PostDate descending,
                ((Post)p.Value.Item1).Title descending
            select (Post)p.Value.Item1).ToList();
    }

    /// <summary>
    /// Count pages in a blog
    /// </summary>
    /// <param name="posts">All posts</param>
    /// <returns>Number of pages</returns>
    private int CountPages(IList<Post> posts)
    {
        int num = posts.Count() / FennicaProject.PostsPerPage;
        if (posts.Count() % FennicaProject.PostsPerPage > 0)
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
        
    /// <inheritdoc cref="FennicaController.GetRoutes"/>
    public override IList<string> GetRoutes(Content content)
    {
        var blog = (Blog)content;

        // routes with and withou page number, and XML feed
        return new List<string>
        {
            content.CanonicalURL + "([0-9]+)/",
            content.CanonicalURL,
            $"/{blog.Language}/rss.xml"
        };
    }

    /// <inheritdoc cref="FennicaController.GetURLs"/>
    public override IList<string> GetURLs(Content content)
    {
        var blog = (Blog)content;
        var list = new List<string>
        {
            $"/{blog.Language}/rss.xml",
            content.CanonicalURL,
        };
        var numPages = CountPages(GetPosts(blog));
        for (int i = 2; i <= numPages; i++)
        {
            list.Add(content.CanonicalURL + $"{i}/");
        }

        return list;
    }

    /// <summary>
    /// Renders a RSS feed with the first page of posts from the blog using .NET's SyndicationFeed class().
    /// </summary>
    /// <param name="blog">Blog to render</param>
    /// <param name="response">HttpResponse to render to</param>
    public async Task RenderRSS(Blog blog, HttpResponse response)
    {
        // take the first page of posts
        var posts = GetPosts(blog);
        if (posts.Count > FennicaProject.PostsPerPage)
        {
            posts = posts.Take(FennicaProject.PostsPerPage).ToList();
        }
            
        // init feed
        var feed = new SyndicationFeed(FennicaProject.MainTitle, _localizer["rss-description"],
            new Uri($"{FennicaProject.PublicBase}/{blog.Language}/"));
        feed.Generator = "Fennica2/Teos";
        feed.Language = blog.Language;
        feed.Copyright = new TextSyndicationContent(FennicaProject.Copyright + " " + FennicaProject.Author);
        feed.LastUpdatedTime = DateTimeOffset.Now;

        // feed entries
        var items = new List<SyndicationItem>();
        foreach (var post in posts)
        {
            var item = new SyndicationItem(post.Title, post.Description,
                new Uri(FennicaProject.PublicBase + post.CanonicalURL),
                FennicaProject.PublicBase + post.CanonicalURL,
                new DateTimeOffset(post.PostDate.ToDateTime(TimeOnly.MinValue)));
            item.PublishDate = item.LastUpdatedTime;
            if (post.TitleImage.Length > 0)
            {
                var link = SyndicationLink.CreateMediaEnclosureLink(new Uri(FennicaProject.PublicBase + post.TitleImage),
                    "image/jpeg", 0);
                item.Links.Add(link);
            }

            items.Add(item);
        }

        feed.Items = items;

        // write out
        response.ContentType = "application/rss+xml; charset=utf-8";
        var xmlWriter = XmlWriter.Create(response.Body, new XmlWriterSettings() { Encoding = Encoding.UTF8, Async = true });
        var rssFormatter = new Rss20FeedFormatter(feed);
        rssFormatter.WriteTo(xmlWriter);
        await xmlWriter.FlushAsync();
    }

    /// <inheritdoc cref="FennicaController.Render"/>
    public override async Task Render(Content content, GroupCollection routeMatch, HttpResponse response)
    {
        var blog = (Blog)content;
        SetLanguage(blog);
            
        // feed
        if (routeMatch[0].Value.EndsWith("/rss.xml"))
        {
            await RenderRSS(blog, response);
            return;
        }
            
        // route without page = page 1
        int page = 1;
        if (routeMatch.Count > 1)
        {
            page = int.Parse(routeMatch[1].Value);
        }

        // get slice of posts to display
        var allPosts = GetPosts(blog);
        IList<Post> posts = new List<Post>();  // default is none
        var start = (page - 1) * FennicaProject.PostsPerPage;
        if (start >= 0 && start < allPosts.Count)
        {
            var count = FennicaProject.PostsPerPage;
            if (start + count > allPosts.Count)
            {
                count = allPosts.Count - start;
            }

            posts = allPosts.GetRange(start, count);
        }

        var layoutParams = new LayoutParams()
        {
            Title = "blog",
            Language = blog.Language,
            BodyClass = "body-blog",
            LanguageVersions = GetLanguageVersionURLs(blog.Name),
            BigHeaderPage = "blog",
            RSSLink = blog.CanonicalURL + "rss.xml"
        };

        await response.WriteAsync(await RazorTemplateEngine.RenderAsync("/Views/Blog.cshtml", blog, new Dictionary<string, object>()
        {
            { "LayoutParams", layoutParams },
            { "Posts", posts },
            { "Page", page },
            { "TotalPages", CountPages(allPosts) },
            { "Pagination", GetPagesForPagination(CountPages(allPosts), page) },
            { "BuildPath", TeosEngine.BuildPath }
        }));
    }
}