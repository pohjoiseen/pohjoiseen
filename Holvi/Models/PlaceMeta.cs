namespace Holvi.Models;

public class PlaceMeta
{
    public string? Directions { get; set; }
    public string? PublicTransport { get; set; }
    public string? Season { get; set; }

    public class PlaceMetaPopulation
    {
        public int Number { get; set; }
        public int Year { get; set; }
    }
    public PlaceMetaPopulation? Population { get; set; }

    public class PlaceMetaArea
    {
        public decimal SqKm { get; set; }
        public bool ExcludesSeaAreas { get; set; }
    }
    public PlaceMetaArea? Area { get; set; }
    
    public enum PlaceMetaTrailDistanceType
    {
        Circular,
        ThereAndBack,
        EndToEnd
    }
    public class PlaceMetaTrailDistance
    {
        public decimal Kilometers { get; set; }
        public PlaceMetaTrailDistanceType Type { get; set; }
    }
    public PlaceMetaTrailDistance? TrailDistance { get; set; }

    public class PlaceMetaEstablished
    {
        public int Year { get; set; }
        public string? Label { get; set; }
        public int? AltYear { get; set; }
        public string? AltLabel { get; set; }
    }
    public PlaceMetaEstablished? Established { get; set; }

    public class PlaceMetaCoatOfArms
    {
        public int PictureId { get; set; }
        public int? OverrideSize { get; set; }
    }
    public PlaceMetaCoatOfArms? CoatOfArms { get; set; }

    public class PlaceMetaLanguagePercent
    {
        public decimal? FinnishPercent { get; set; }
        public decimal? SwedishPercent { get; set; }
        public decimal? SamiPercent { get; set; }
        public decimal? ForeignPercent { get; set; }
        public int Year { get; set; }
    }
    public PlaceMetaLanguagePercent? LanguagePercent { get; set; }
    
    // hidden
    public string? FlagEmoji { get; set; }
    public string? MapType { get; set; }
}