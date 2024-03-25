using System.Text.Json.Serialization;

namespace KoTi.Models;

public class Tag
{
    public int Id { get; set; }

    public string Name { get; set; }
    
    public bool IsPrivate { get; set; }

    [JsonIgnore]
    public IList<Picture> Pictures { get; } = [];
    
    [JsonIgnore]
    public IList<Place> Places { get; } = [];
}