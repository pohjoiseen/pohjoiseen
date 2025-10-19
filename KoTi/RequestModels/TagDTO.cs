using KoTi.Models;

namespace KoTi.RequestModels;

public class TagDTO
{
    public int? Id { get; set; }

    public string Name { get; set; } = "";
    
    public bool IsPrivate { get; set; }

    public void ToModel(Tag tag)
    {
        tag.Name = Name;
        tag.IsPrivate = IsPrivate;
    }
}
