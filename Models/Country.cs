namespace KoTi.Models;

public class Country
{
    public int Id { get; set; }

    public string Name { get; set; }

    public string FlagEmoji { get; set; }
    
    public string MapType { get; set; }

    public int Order { get; set; }
    
    public IList<Region> Regions { get; set; }
}