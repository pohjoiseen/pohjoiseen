namespace KoTi.ViewModels;

public class ArticleViewModel : AbstractContentViewModel
{
    public override string KindPlural => "Articles";

    public string Name { get; set; } = "";
    
    public override string FennicaURL => $"/{Language}/article/{Name}/";
}