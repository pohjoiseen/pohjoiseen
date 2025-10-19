using System.Text.RegularExpressions;

namespace Fennica;

/// <summary>
/// Standalone page on Fennica website.
/// </summary>
public class Page : FennicaContent
{
    public string Title { get; set; } = "";

    private string _name = "";
    public override string Name
    {
        get
        {
            return _name;
        }
        set
        {
            // set language from the filename
            var match = Regex.Match(value, "\\.([^.]+)\\.article\\.md$");
            if (match.Success)
            {
                Language = match.Groups[1].Value;
            }
            _name = value;
        }
    }

    public override string CanonicalURL
    {
        get
        {
            var basename = Path.GetFileName(_name);
            basename = basename[..basename.IndexOf(".")];
            return $"/{Language}/article/{basename}/";
        }
    }
}