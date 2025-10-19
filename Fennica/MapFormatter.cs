using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text.Unicode;
using System.Web;
using GeoJSON.Text.Feature;
using GeoJSON.Text.Geometry;
using Teos;

namespace Fennica;

/// <summary>
/// Converts pseudo-markup <!--map-->...<!--/map--> into markup usable by map browser (see Client/maps.ts).
/// 
/// Expects HTML:
/// <!--map map-name-->
/// Sidebar content
/// <!--/map-->
///
/// Map name is optional.  Default is "index".
/// Will replace every such block by appropriate markup.  Content HTML is pasted as is.
/// </summary>
public class MapFormatter : IContentFormatter
{
    private ITeosEngine _teosEngine;

    public void SetTeosEngine(ITeosEngine teosEngine)
    {
        _teosEngine = teosEngine;
    }

    /// <summary>
    /// Generates GeoJSON data for a map, in 16 zoom levels.
    /// Maps are not independent content items, rather GeoJSON data markup for posts is gathered.
    /// </summary>
    /// <param name="mapName">Map name to filter by</param>
    /// <param name="language">Language to filter by</param>
    /// <returns>Array of 16 GeoJSON FeatureCollection objects</returns>
    public FeatureCollection[] GenerateGeoJSON(string mapName, string language)
    {
        FeatureCollection[] geoJSONs = new FeatureCollection[16];
            
        var posts = (from p in _teosEngine.AllContent
            where p.Value.Item1 is Post post
                  && post.Language == language
            select (Post)p.Value.Item1).ToList();

        for (int i = 0; i < 16; i++)
        {
            geoJSONs[i] = new FeatureCollection();
                
            // match posts having geo defined for this zoom level
            int zoom = i;
            var postsForZoomLevel = from p in posts
                where p.Geo.Any(geo => geo.Maps != null && geo.Maps.Contains(mapName) && geo.Zoom == zoom)
                select p;

            foreach (var post in postsForZoomLevel)
            {
                foreach (var geo in post.Geo)
                {
                    // check again that matches
                    if (geo.Maps == null || !geo.Maps.Contains(mapName) || geo.Zoom != zoom)
                    {
                        continue;
                    }

                    var feature = new Feature {
                        // rather awkward line, but id = POSTNAME#LNG-LAT, where postname has directory part and .en.post.md part removed,
                        // and lat/lng always have dots, not commas
                        Id = $"{post.Name[(post.Name.LastIndexOf('/') + 1)..].Replace( $".{language}.post.md", "")}#{geo.Lng}-{geo.Lat}".Replace(',', '.'),
                        Geometry = new Point(new Position(geo.Lat, geo.Lng)),
                        Properties = new Dictionary<string, object> {
                            { "title", (geo.Title != null && geo.Title.Length > 0 ? geo.Title : post.Title) },
                            { "icon", geo.Icon },
                            { "url", post.CanonicalURL },
                            { "anchor", geo.Anchor }
                        }
                    };
                    geoJSONs[i].Features.Add(feature);
                }
            }
        }

        return geoJSONs;
    }
        
    /// <summary>
    /// Actually formats map blocks.
    /// </summary>
    /// <param name="html">Input HTML</param>
    /// <param name="path">Content item path</param>
    /// <returns>Processed HTML</returns>
    public Task<string> FormatHTML(string html, string path)
    {
        bool matched;
        const string openTag = "<!--map\\s*([^>]+)?-->", closeTag = "<!--/map-->";

        var content = (FennicaContent)_teosEngine.AllContent[path].Item1;
            
        do
        {
            matched = false;
            
            // find next <!--map-->...<!--/map--> block
            Match openMatch = Regex.Match(html, openTag);
            if (!openMatch.Success)
            {
                break;
            }

            int openIndex = openMatch.Index;

            // map name from opening tag
            string[] mapNames = { "index" };
            if (openMatch.Groups[1].Value.Length > 0)
            {
                mapNames = openMatch.Groups[1].Value.Split(",");
            }

            int closeIndex = html.IndexOf(closeTag);
            if (closeIndex == -1)
            {
                break;
            }

            matched = true;
                
            // geoJSON data
            var containersHTML = "";
            var first = true;
            foreach (var mapName in mapNames)
            {
                var geoJSONs = GenerateGeoJSON(mapName, content.Language);
                string geoJSONsSerialized = JsonSerializer.Serialize(geoJSONs, new JsonSerializerOptions
                {
                    Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic),
                });
                containersHTML += $"<div class=\"leaflet-container\" data-map=\"{mapName}\" data-geojson=\"{HttpUtility.HtmlEncode(geoJSONsSerialized)}\"" + 
                                  (!first ? " style=\"display: none;\"" : "") + "></div>";
                first = false;
            }
                
            // wrapped content
            string wrappedHtml = html[(openIndex + openMatch.Length)..closeIndex].Trim();
                
            // replacement HTML
            StringBuilder output = new StringBuilder();
            output.Append($"<div class=\"mapview-wrapper {(wrappedHtml.Length > 0 ? "with-aside" : "no-aside")}\" data-maps=\"{string.Join(',', mapNames)}\">");
            output.Append(containersHTML);
            if (wrappedHtml.Length > 0)
            {
                output.Append($"<div class=\"mapview-aside\">{wrappedHtml}</div>");
            }
            output.Append("</div>");  // .mapview.wrapper

            // replace original markup with generated
            html = html[..openIndex] + output + html[(closeIndex + closeTag.Length)..];
                
            // continue until no more blocks found
        } while (matched);

        return Task.FromResult(html);
    }
}