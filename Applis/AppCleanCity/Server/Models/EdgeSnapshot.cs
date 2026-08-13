using NetTopologySuite.Geometries;

namespace CortexiaAuth.Api.Models;

/// <summary>
/// Position ponctuelle d'une "suitcase" le long d'un edge, ou parfois au niveau d'une place
/// (aggregated_snapshots). Table partitionnée par mois sur MeasuredAt : volume attendu élevé, append-only.
/// </summary>
public class EdgeSnapshot
{
    public long Id { get; set; }

    /// <summary>Renseigné quand l'id Cortexia référence un edge "(u, v, key)".</summary>
    public long? EdgeU { get; set; }
    public long? EdgeV { get; set; }
    public short? EdgeKey { get; set; }

    /// <summary>Renseigné quand l'id Cortexia référence une place (identifiant Elasticsearch), pas un edge.</summary>
    public string? PlaceId { get; set; }

    public float Direction { get; set; }
    public float SpeedMs { get; set; }
    public string SuitcaseId { get; set; } = string.Empty;
    public Point Location { get; set; } = null!;
    public short[] Details { get; set; } = [];
    public DateTime MeasuredAt { get; set; }
    public DateTime PostedAt { get; set; }
    public int CityId { get; set; }

    /// <summary>
    /// Numéro d'itinéraire (1, 2, ...) au sein de la journée de la suitcase : une nouvelle fenêtre
    /// de moins de 7h démarre à chaque relevé distant de plus de 7h du début de la fenêtre en cours.
    /// Recalculé entièrement après chaque import (voir IDataImportService.AssignItineraryNumbersAsync),
    /// donc jamais fiable pendant/juste après un import partiel.
    /// </summary>
    public int? ItineraryNumber { get; set; }
}
