using Holvi.Models;

namespace KoTi.ViewModels;

public class BookPickerViewModel
{
    public required string ComponentId { get; set; }
    public required string FieldName { get; set; }
    public required Book? Book { get; set; }
    public required string Language { get; set; }
}