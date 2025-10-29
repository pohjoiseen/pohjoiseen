namespace Fennica3.ViewModels;

public class StatusCodeModel
{
    public int StatusCode { get; set; }
    
    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId) && StatusCode != 404;
    
    public required LayoutParams LayoutParams { get; set; }
}