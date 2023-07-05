using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Fennica2;

/// <summary>
/// Post for Fennica website, the main content type.
/// </summary>
public class Post : FennicaContent
{
    /// <summary>
    /// Helper class: Geo point definition to show on a map, one or more can be associated to a post.
    /// </summary>
    public struct GeoPoint
    {
        public GeoPoint() {}
        
        public string Title = "";
        public string Subtitle = "";
        public string Description = "";
        public string TitleImage = "";
        public string Anchor = "";
        public double Lat = 0.0;
        public double Lng = 0.0;
        public int Zoom = 0;
        public string Icon = "";
        public string[] Maps = new string[] { };
        public Link[] Links = new Link[] { };
    }

    /// <summary>
    /// Helper struct: Coat of arms image definition, one or more can be associated to a post.
    /// </summary>
    public struct CoatOfArms
    {
        public string Url { get; set; }
        public int Size { get; set; }
    }

    /// <summary>
    /// Helper class: Link definition for GeoPoint
    /// </summary>
    public struct Link
    {
        public string Label { get; set; }
        public string Path { get; set; }
    }
    
    public string Title { get; set; } = "";

    public string Description { get; set; } = "";
    
    public bool Mini { get; set; }

    public string TitleImage { get; set; } = "";

    /// <summary>
    /// For title images as header background, sets background-position-y
    /// </summary>
    public int TitleImageOffsetY { get; set; }

    /// <summary>
    /// true = title image (if present) is shown as a regular image in the beginning of the post
    /// false = title image (if present) is shown as a background image in the big header
    /// </summary>
    public bool TitleImageInText { get; set; }

    /// <summary>
    /// Used when titleImageInText=true
    /// </summary>
    public string TitleImageCaption { get; set; } = "";

    /// <summary>
    /// The date from post filename
    /// </summary>
    public DateOnly PostDate { get; set; }

    /// <summary>
    /// Free-form description of the post timing, shown in header ("Pictures from May 2018, text from February 2023")
    /// </summary>
    public string DateDescription { get; set; } = "";

    /// <summary>
    /// Free-form short description of where approximately the place mentioned is (not precise address or directions)
    /// </summary>
    public string LocationDescription { get; set; } = "";

    /// <summary>
    /// Address of the place described (a line or a paragraph)
    /// </summary>
    public string Address { get; set; } = "";

    /// <summary>
    /// Public transport accessibility of the place described (if at all possible)
    /// </summary>
    public string PublicTransport { get; set; } = "";

    /// <summary>
    /// Link to a Twitter thread
    /// </summary>
    public string Twitter { get; set; } = "";

    /// <summary>
    /// One of more coats of arms
    /// </summary>
    public CoatOfArms[] CoatsOfArms { get; set; } = Array.Empty<CoatOfArms>();

    public GeoPoint[] Geo { get; set; } = Array.Empty<GeoPoint>();

    private string _name = "";
    public override string Name
    {
        get => _name;
        set
        {
            // set language and date from the filename
            var match = Regex.Match(value, "([0-9]{4})-([0-9]{2})-([0-9]{2})-[^.]+\\.([^.]+)\\.post\\.md$");
            if (match.Success)
            {
                PostDate = new DateOnly(int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value), int.Parse(match.Groups[3].Value));
                Language = match.Groups[4].Value;
            }
            _name = value;
        }
    }

    public override string CanonicalURL
    {
        get
        {
            var basename = Path.GetFileName(_name);
            var match = Regex.Match(basename, "^[0-9]{4}-[0-9]{2}-[0-9]{2}-([^.]+)*\\.");
            Debug.Assert(match.Success);
            return $"/{Language}/{PostDate.ToString("yyyy\\/MM\\/dd")}/{match.Groups[1]}/";
        }
    }

    /// <summary>
    /// Gets .1x version of the title image, if the image is set.
    /// </summary>
    /// <returns>Image path or empty string</returns>
    public string GetTitleImage1X()
    {
        if (TitleImage.Length == 0)
        {
            return "";
        }

        int dotIndex = TitleImage.LastIndexOf('.');
        if (dotIndex != -1)
        {
            return TitleImage[..dotIndex] + ".1x." + TitleImage[(dotIndex + 1)..];
        }

        return TitleImage;
    }

    /// <summary>
    /// Gets long post date (but without week day) for current locale. 
    /// </summary>
    /// <returns>Post date string</returns>
    public string GetFormattedPostDate()
    {
        return PostDate.ToLongDateString()
            .Replace(DateTimeFormatInfo.CurrentInfo.GetDayName(PostDate.DayOfWeek), "")
            .TrimStart(", ".ToCharArray());
    }

    /// <summary>
    /// Creates a shallow clone of the post with HTML set to null.  A bit hacky method used
    /// to generate JSON versions of the posts (for dynamic loading in maps) without actual HTML.
    /// </summary>
    /// <returns>Copy</returns>
    public Post GetShallowCopyWithoutHTML()
    {
        Post copy = (Post) MemberwiseClone();
        copy.HTML = null;
        return copy;
    }
}
