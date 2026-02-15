namespace KoTi.ViewModels;

public class BookViewModel : AbstractContentViewModel
{
    public override string KindLink => "book";
    public override string KindPlural => "Books";

    public string Name { get; set; } = "";
    
    public int? TitlePictureId { get; set; }
    
    public IList<PostChild> Posts { get; set; } = [];
    
    public override string FennicaURL => $"/{Language}/{Name}/";
    
    public record PostChild
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public bool Draft { get; set; }
    }
}