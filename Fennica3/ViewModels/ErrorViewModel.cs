namespace Fennica3.ViewModels;

public class ErrorViewModel
{
    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    
    public required LayoutParams LayoutParams { get; set; }
}