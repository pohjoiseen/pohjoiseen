using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Holvi.Models;

namespace KoTi.RequestModels;

public class ArticleRequestDTO
{
    [Required] public string Name { get; set; } = "";
    
    public bool Draft { get; set; }
    
    public string ContentMD { get; set; } = "";

    [Required] public string Title { get; set; } = "";

    public void ToModel(Article model)
    {
        model.Name = Name;
        model.ContentMD = ContentMD;
        model.Title = Title;
        model.Draft = Draft;
    }
}