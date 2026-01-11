using Holvi.Models;

namespace KoTi.ViewModels;

public class PostViewModel : AbstractContentViewModel
{
    public override string KindPlural => "Posts";

    public DateOnly Date { get; set; }
    
    public string Name { get; set; } = "";
    
    public bool Mini { get; set; }

    public string? Description { get; set; } = "";
    
    public string? DateDescription { get; set; } = "";
    public string? LocationDescription { get; set; } = "";
    
    public int? TitlePictureId { get; set; }
    public int? TitleImageOffsetY { get; set; }
    public bool TitleImageInText { get; set; }
    public string? TitleImageCaption { get; set; }

    public IList<Post.CoatOfArms> CoatsOfArms { get; set; } = new List<Post.CoatOfArms>();
    
    public string ViewStatePostTab { get; set; } = "main";
    
    public override string FennicaURL => $"/{Language}/{Date.ToString("yyyy")}/{Date.ToString("MM")}/{Date.ToString("dd")}/{Name}/";
}