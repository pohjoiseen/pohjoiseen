using System.Text.Json.Serialization;

namespace Holvi.Models;

public class Region
{
    public int Id { get; set; }

    public int CountryId { get; set; }

    [JsonIgnore] public Country Country { get; set; } = null!;

    public string Name { get; set; } = "";

    public int Order { get; set; }

    [JsonIgnore] public IList<Area> Areas { get; set; } = [];
}
