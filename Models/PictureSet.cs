using System.Text.Json.Serialization;

namespace KoTi.Models;

public class PictureSet
{
    public int Id { get; set; }

    public string Name { get; set; }
    
    public bool IsPrivate { get; set; }
    
    public int? ParentId { get; set; }
    [JsonIgnore]
    public PictureSet? Parent { get; set; }
    
    public IList<PictureSet> Children { get; set; }
    
    public IList<Picture> Pictures { get; set; }
}