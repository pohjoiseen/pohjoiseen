using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace KoTi.Models;

[Index(nameof(Name))]
public class Article
{
    public int Id { get; set; }

    public bool Draft { get; set; }

    [MaxLength(255)]
    public string Name { get; set; } = "";
    
    public string ContentMD { get; set; } = "";

    [MaxLength(2)]
    public string Language { get; set; } = "";
    
    public DateTime UpdatedAt { get; set; }
}