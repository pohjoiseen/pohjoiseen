using System.ComponentModel.DataAnnotations;
using KoTi.Models;

namespace KoTi.RequestModels;

public class PictureSetDTO
{
    [Required] public string Name { get; set; }
    
    [Required] public bool IsPrivate { get; set; }
    
    public int? ParentId { get; set; }

    public void ToModel(PictureSet set)
    {
        set.Name = Name;
        set.IsPrivate = IsPrivate;
        set.ParentId = ParentId;
    }
}