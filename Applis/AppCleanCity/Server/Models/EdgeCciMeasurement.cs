namespace CortexiaAuth.Api.Models;

/// <summary>
/// Indice de propreté (CCI) mesuré pour un edge à un instant donné (edges_and_places_cci).
/// Table partitionnée par mois sur MeasuredAt, comme EdgeSnapshot.
/// </summary>
public class EdgeCciMeasurement
{
    public long Id { get; set; }

    /// <summary>Renseigné quand l'id Cortexia référence un edge "(u, v, key)".</summary>
    public long? EdgeU { get; set; }
    public long? EdgeV { get; set; }
    public short? EdgeKey { get; set; }

    /// <summary>Renseigné quand l'id Cortexia référence une place (identifiant Elasticsearch), pas un edge.</summary>
    public string? PlaceId { get; set; }

    public DateTime MeasuredAt { get; set; }
    public DateTime PostedAt { get; set; }
    public float Direction { get; set; }
    public float? Cci { get; set; }
    public float? CciCustom { get; set; }
    public bool HasMeasure { get; set; }
    public string[] SuitcaseIds { get; set; } = [];

    public float? RateLeaves { get; set; }
    public float? RateCigarettes { get; set; }
    public float? RateGums { get; set; }
    public float? RatePapers { get; set; }
    public float? RateGrits { get; set; }
    public float? RateBottles { get; set; }
    public float? RateExcrements { get; set; }
    public float? RateGlassDebris { get; set; }
    public float? RateSyringues { get; set; }

    /// <summary>custom_cci_per_types : types définis par l'utilisateur, non figés à la compilation.</summary>
    public string CustomCciPerTypesJson { get; set; } = "{}";
}
