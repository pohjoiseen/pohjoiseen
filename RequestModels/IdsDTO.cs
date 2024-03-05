using System.ComponentModel.DataAnnotations;

namespace KoTi.RequestModels;

public class IdsDTO
{
    [Required] public int[] Ids { get; set; } = Array.Empty<int>();
}
