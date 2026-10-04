using KoTi.LiveJournal;

namespace KoTi.ViewModels.Posts;

public class LJCrosspostViewModel
{
    public int Id { get; init; }
    public string Language { get; init; } = "";
    public LJCrosspostOptions Options { get; init; } = new();
    public string Html { get; init; } = "";
}
