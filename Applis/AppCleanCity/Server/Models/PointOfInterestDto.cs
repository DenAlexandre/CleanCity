namespace CortexiaAuth.Api.Models;

public record PointOfInterestDto(Guid Id, string Name, string? Description, string Category, double Latitude, double Longitude, DateTime CreatedAtUtc);

/// <summary>Note (Cci moyen, rayon 500m) d'un point d'intérêt sur une période donnée.</summary>
public record PointOfInterestScoreDto(Guid Id, string Name, string? Description, string Category, double? AverageCci);

/// <summary>Répartition par type des objets détectés à proximité (rayon 500m) d'un point d'intérêt.</summary>
public record PointOfInterestObjectBreakdownDto(short TypeCode, string TypeName, int Count);
