using System.ComponentModel.DataAnnotations;
using KoTi.Models;

namespace KoTi.DTO;

public class OrderDTO
{
    [Required] public int[] Ids { get; set; } = Array.Empty<int>();
}
