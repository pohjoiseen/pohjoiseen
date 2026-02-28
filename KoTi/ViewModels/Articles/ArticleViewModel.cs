namespace KoTi.ViewModels.Articles;

public class ArticleViewModel : AbstractContentViewModel
{
    public override string KindLink => "article";
    public override string KindPlural => "Articles";

    public string Name { get; set; } = "";
    
    public override string FennicaURL => $"/{Language}/article/{Name}/";
}