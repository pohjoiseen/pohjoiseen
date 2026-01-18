namespace Holvi.Models;

public class AreaLocalization
{
    public int Id { get; set; }
    public int AreaId { get; set; }
    public Area Area { get; set; } = null!;
    public string Language { get; set; } = "";
    public string Name { get; set; } = "";
    public string Alias { get; set; } = "";
    public string Notes { get; set; } = "";
    public string Links { get; set; } = "";
    public int Order { get; set; }
    public bool IsPrivate { get; set; }
    public DateTime UpdatedAt { get; set; }
}

