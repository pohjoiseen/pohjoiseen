using System.Text.RegularExpressions;

namespace Fennica2;

/// <summary>
/// Fennica2 currently has only a single blog (in several language versions).
/// </summary>
public class Blog : FennicaContent
{
    private string _name = "";
    public override string Name
    {
        get => _name;
        set
        {
            // set language from the filename
            var match = Regex.Match(value, "\\.([^.]+)\\.blog\\.md$");
            if (match.Success)
            {
                Language = match.Groups[1].Value;
            }
            _name = value;
        }
    }

    public override string CanonicalURL =>
        // don't actually use name
        "/" + Language + "/";

    public string GetLinkToPage(int page)
    {
        string link = CanonicalURL;
        if (page > 1)
        {
            link += page + "/";
        }

        return link;
    }
}
