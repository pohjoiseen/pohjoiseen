using Microsoft.EntityFrameworkCore;

namespace Holvi.Models;

[Index(nameof(UrlFrom), IsUnique = true)]
public class Redirect
{
    public int Id { get; set; }

    public string UrlFrom { get; set; } = "";
    
    public string UrlTo { get; set; } = "";
}