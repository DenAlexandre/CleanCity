namespace CortexiaAuth.Api.Models;

/// <summary>
/// Un itinéraire = les relevés d'une suitcase sur une même journée, découpés en fenêtres
/// glissantes de moins de 7h à partir du premier relevé du jour (une suitcase peut faire
/// plusieurs tournées par jour si l'écart dépasse 7h).
/// </summary>
public record ItineraryDto(
    string SuitcaseId,
    DateOnly Day,
    int ItineraryIndex,
    DateTime StartTime,
    DateTime EndTime,
    int ObjectCount,
    IReadOnlyList<string> Streets,
    double? AverageCci);

/// <summary>
/// Détail d'un tronçon parcouru pendant un itinéraire : la rue à laquelle il appartient, sa note
/// de propreté (Cci) et les objets qui y ont été détectés pendant cet itinéraire.
/// </summary>
public record ItineraryStreetDetailDto(string Street, int TotalObjects, double? AverageCci, IReadOnlyList<MeasurementTypeBreakdownDto> Objects);
