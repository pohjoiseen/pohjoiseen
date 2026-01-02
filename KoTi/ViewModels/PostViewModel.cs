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
    
    public override string FennicaURL => $"/{Language}/{Date.ToString("yyyy")}/{Date.ToString("MM")}/{Date.ToString("dd")}/{Name}/";
}