using Razor.Templating.Core;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Text.Unicode;
using Teos;

namespace Fennica2;

/// <summary>
/// Controller for Post content.
/// </summary>
public class PostController : FennicaController
{
    public override IList<string> GetRoutes(Content content)
    {
        var post = (Post)content;

        return new List<string>()
        {
            content.CanonicalURL,
            // /path/to/2020-01-01-post.en.post.md -> /ru/json/2020-01-01-post.json
            $"/{post.Language}/json/{post.Name[(post.Name.LastIndexOf('/') + 1)..].Replace($".{post.Language}.post.md", ".json")}"
        };
    }

    /// <summary>
    /// Formats in post: main body, description, geo point description.
    /// </summary>
    /// <param name="content">Post</param>
    public override async Task ApplyFormatting(Content content)
    {
        var post = (Post)content;
        if (post.Description.Length > 0)
        {
            post.Description = await TeosEngine.FormatHTML(post.Description, post.Name);
        }

        if (post.Geo != null)
        {
            post.Geo = await Task.WhenAll(post.Geo.Select(async geo =>
            {
                if (geo.Description.Length > 0)
                {
                    geo.Description = await TeosEngine.FormatHTML(geo.Description, post.Name);
                }

                if (geo.Links != null) 
                {
                    geo.Links = geo.Links.Select(link =>
                    {
                        if (link.Path != null)
                        {
                            // this is for frontend usage, so convert to public URL
                            link.Path = TeosEngine.AllContent[TeosEngine.ResolvePath(link.Path, post.Name)].Item1
                                .CanonicalURL;
                        }

                        return link;
                    }).ToArray();
                }

                return geo;
            }));
        }
            
        // prepend title image manually if that is requested
        if (post.TitleImage.Length > 0 && post.TitleImageInText)
        {
            post.HTML =
                $"<p><img alt=\"{post.TitleImageCaption}\" src=\"{post.TitleImage}\" /></p>\n\n{post.HTML}";
        }
    }
        
    /// <inheritdoc cref="FennicaController.Render"/>
    public override async Task Render(Content content, GroupCollection routeMatch, HttpResponse response)
    {
        var post = (Post)content;            
        SetLanguage(post);

        // JSON serialization for maps/geo
        if (routeMatch[0].Value.EndsWith(".json"))
        {
            await response.WriteAsJsonAsync(post.GetShallowCopyWithoutHTML(), new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic),
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new DateOnlyJsonConverter() },
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                IncludeFields = true
            });
            return;
        }

        // next/prev post: same language, sort by date then title, next one after this one
        var allPosts =
            (from p in TeosEngine.AllContent
                where p.Value.Item1 as Post != null
                select p.Value.Item1 as Post).ToList();
        var prevPost =
            (from p in allPosts
                where p.Language == post.Language
                      && (p.PostDate < post.PostDate || (p.PostDate.Equals(post.PostDate) && p.Title.CompareTo(post.Title) < 0))
                      && p.Name != post.Name
                orderby p.PostDate descending, p.Title descending
                select p).FirstOrDefault();
        var nextPost =
            (from p in allPosts
                where p.Language == post.Language
                      && (p.PostDate > post.PostDate || (p.PostDate.Equals(post.PostDate) && p.Title.CompareTo(post.Title) > 0))
                      && p.Name != post.Name
                orderby p.PostDate ascending, p.Title ascending
                select p).FirstOrDefault();

        var layoutParams = new LayoutParams()
        {
            Title = post.Title,
            Language = post.Language,
            BodyClass = "body-post",
            TitleImage = post.TitleImage,
            LanguageVersions = GetLanguageVersionURLs(post.Name),
            PrevPath = prevPost == null ? "" : prevPost.CanonicalURL,
            PrevTitle = prevPost == null ? "" : prevPost.Title,
            NextPath = nextPost == null ? "" : nextPost.CanonicalURL,
            NextTitle = nextPost == null ? "" : nextPost.Title
        };

        await response.WriteAsync(await RazorTemplateEngine.RenderAsync("/Views/Post.cshtml", post, new Dictionary<string, object>()
        {
            { "LayoutParams", layoutParams },
            { "PrevPost", prevPost },
            { "NextPost", nextPost }
        }));
    }
    
    /// <summary>
    /// Used for post JSON seralization.
    /// </summary>
    private sealed class DateOnlyJsonConverter : JsonConverter<DateOnly>
    {
        public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return DateOnly.FromDateTime(reader.GetDateTime());
        }

        public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
        {
            var isoDate = value.ToString("O");
            writer.WriteStringValue(isoDate);
        }
    }
}
