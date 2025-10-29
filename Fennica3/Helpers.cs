using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Text.Unicode;
using System.Web;
using GeoJSON.Text.Feature;
using GeoJSON.Text.Geometry;
using Holvi;
using Holvi.Models;

namespace Fennica3;

/// <summary>
/// Provides a few mostly unrelated methods for HTML generation, to be referenced from both controllers and views.
/// </summary>
/// <param name="dbContext">Database</param>
/// <param name="linkGenerator">ASP.NET LinkGenerator</param>
/// <param name="logger">.NET logger</param>
/// <param name="configuration">.NET Core IConfiguration</param>
public class Helpers(HolviDbContext dbContext, LinkGenerator linkGenerator, 
                     ILogger<Helpers> logger, IConfiguration configuration)
{
    /// <summary>
    /// Generates a canonical link to a post 
    /// </summary>
    /// <param name="post"></param>
    /// <returns>Link (without host)</returns>
    public string PostLink(Post post)
    {
        return linkGenerator.GetPathByAction("Post", "Blog", new
        {
            language = post.Language,
            year = post.Date.Year,
            month = post.Date.ToString("MM"),
            day = post.Date.ToString("dd"),
            name = post.Name
        })!;
    }
    
    /// <summary>
    /// Generates a canonical link to an article 
    /// </summary>
    /// <param name="article"></param>
    /// <returns>Link (without host)</returns>
    public string ArticleLink(Article article)
    {
        return linkGenerator.GetPathByAction("View", "Article", new
        {
            language = article.Language,
            name = article.Name
        })!;
    }
    
    /// <summary>
    /// Generates GeoJSON data for blog maps, in 16 zoom levels.
    /// </summary>
    /// <param name="language">Language to filter by</param>
    /// <returns>Map of map name -> 16 GeoJSON FeatureCollection objects</returns>
    public IDictionary<string, FeatureCollection[]> GenerateGeoJSONFromPosts(string language)
    {
        var geoJSONs = new Dictionary<string, FeatureCollection[]>(); 
        
        var posts = dbContext.Posts
            .Where(p => p.Language == language)
            .Where(p => configuration["Fennica3:WithDrafts"] != null || !p.Draft)
            .ToList();

        foreach (var post in posts)
        {
            if (post.Geo == null)
            {
                continue;
            }
            
            foreach (var geo in post.Geo)
            {
                if (geo.Maps == null)
                {
                    continue;
                }
                
                var feature = new Feature {
                    // rather awkward line, but id = POSTNAME#LNG-LAT, where postname is post.toString()
                    // and lat/lng always have dots, not commas
                    Id = $"{post}#{geo.Lng}-{geo.Lat}".Replace(',', '.'),
                    Geometry = new Point(new Position(geo.Lat, geo.Lng)),
                    Properties = new Dictionary<string, object> {
                        { "title", !string.IsNullOrEmpty(geo.Title) ? geo.Title : post.Title },
                        { "url", PostLink(post) }
                    }
                };
                if (geo.Icon != null)
                {
                    feature.Properties.Add("icon", geo.Icon);
                }

                if (geo.Anchor != null)
                {
                    feature.Properties.Add("anchor", geo.Anchor);
                }
                
                foreach (var map in geo.Maps)
                {
                    if (!Fennica3.Maps.Contains(map))
                    {
                        continue;
                    }
                    
                    if (!geoJSONs.ContainsKey(map))
                    {
                        geoJSONs.Add(map, new FeatureCollection[16]);
                        for (int i = 0; i < 16; i++)
                        {
                            geoJSONs[map][i] = new FeatureCollection();
                        }
                    }
                    geoJSONs[map][geo.Zoom ?? 0].Features.Add(feature);
                }
            }
        }

        return geoJSONs;
    }
    
    public string BeginPostMaps(string language)
    {
        var geoJSONs = GenerateGeoJSONFromPosts(language);
        var containersHTML = "";
        var first = true;
        foreach (var (mapName, geoJSON) in geoJSONs)
        {
            string geoJSONSerialized = JsonSerializer.Serialize(geoJSON, new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic),
            });
            containersHTML += $"<div class=\"leaflet-container\" data-map=\"{mapName}\" data-geojson=\"{HttpUtility.HtmlEncode(geoJSONSerialized)}\"" + 
                              (!first ? " style=\"display: none;\"" : "") + "></div>";
            first = false;
        }
        
        StringBuilder output = new StringBuilder();
        output.Append($"<div class=\"mapview-wrapper with-aside\" data-maps=\"{string.Join(',', geoJSONs.Keys)}\">");
        output.Append(containersHTML);
        output.Append("<div class=\"mapview-aside\">");
        return output.ToString();
    }

    public string EndPostMaps() => "</div></div>\n";
    
    /// <summary>
    /// Resolves "picture:XXX" URLs for coats of arms to actual links.
    /// </summary>
    /// <param name="post">Post with COAs</param>
    /// <returns>COA records with correct URLs</returns>
    public IEnumerable<Post.CoatOfArms> ResolveCoatsOfArms(Post post)
    {
        if (post.CoatsOfArms == null)
        {
            return new List<Post.CoatOfArms>();
        }

        return post.CoatsOfArms.Select(coa =>
        {
            if (coa.Url.StartsWith("picture:"))
            {
                var match = Regex.Match(coa.Url, "picture:([0-9]+)");
                if (!match.Success)
                {
                    logger.LogWarning("Malformed coat of arms link: {src}", coa.Url);
                    return coa;
                }

                var pictureId = Int32.Parse(match.Groups[1].Value);

                var picture = dbContext.Pictures.FirstOrDefault(p => p.Id == pictureId);
                if (picture == null)
                {
                    logger.LogWarning("Picture not found for coat of arms link: {src}", coa.Url);
                    return coa;
                }

                return coa with { Url = picture.Url };
            }

            return coa;
        });
    }
    
    /// <summary>
    /// Resolves "picture:XXX" URLs for title images and "post:XXX" for links to actual links.
    /// </summary>
    /// <param name="post">Post with geo points</param>
    /// <returns>Geo records with correct URLs</returns>
    public IEnumerable<Post.GeoPoint> ResolveGeos(Post post)
    {
        if (post.Geo == null)
        {
            return new List<Post.GeoPoint>();
        }

        return post.Geo.Select(geo =>
        {
            // resolve picture
            // note, "details" size is well enough here
            string titleImage = "";
            if (geo.TitleImage != null)
            {
                // picture: link
                if (geo.TitleImage.StartsWith("picture:"))
                {
                    var match = Regex.Match(geo.TitleImage, "picture:([0-9]+)");
                    if (!match.Success)
                    {
                        logger.LogWarning("Malformed geo title image link: {src}", geo.TitleImage);
                    }
                    else
                    {
                        var pictureId = Int32.Parse(match.Groups[1].Value);
                        var picture = dbContext.Pictures.FirstOrDefault(p => p.Id == pictureId);
                        if (picture == null)
                        {
                            logger.LogWarning("Picture not found for geo title image link: {src}", geo.TitleImage);
                        }
                        else
                        {
                            titleImage = picture.DetailsUrl;
                        }
                    }
                }
                else
                {
                    // any other link, keep as is
                    titleImage = geo.TitleImage;
                }
            }
            else if (post.TitlePicture != null)
            {
                // no link, use post title picture if available 
                titleImage = post.TitlePicture.DetailsUrl;
            }

            var links = geo.Links?.Select(l =>
            {
                if (l.Path.StartsWith("post:"))
                {
                    var match = Regex.Match(l.Path, "post:([0-9]+)");
                    if (!match.Success)
                    {
                        logger.LogWarning("Malformed geo post link: {src}", l.Path);
                    }
                    else
                    {
                        var postId = Int32.Parse(match.Groups[1].Value);
                        var linkedPost = dbContext.Posts.FirstOrDefault(p => p.Id == postId);
                        if (linkedPost == null)
                        {
                            logger.LogWarning("Post not found for geo post link: {src}", l.Path);
                        }
                        else
                        {
                            return l with { Path = PostLink(linkedPost) };
                        }
                    }
                }

                return l;
            });

            return geo with
            {
                TitleImage = titleImage,
                Links = links?.ToList()
            };
        });
    }
    
    /// <summary>
    /// Used for post JSON seralization.
    /// </summary>
    public sealed class DateOnlyJsonConverter : JsonConverter<DateOnly>
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