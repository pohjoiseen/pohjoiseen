using Holvi.Models;

namespace KoTi.ViewModels.Pictures;

public class PicturePickerViewModel
{
    public required string ComponentId { get; set; }
    public required string FieldName { get; set; }
    public bool StringField { get; set; }
    public required Picture? Picture { get; set; }
}