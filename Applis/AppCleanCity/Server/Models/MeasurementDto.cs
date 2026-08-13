namespace CortexiaAuth.Api.Models;

public record MeasurementDto(long SnapshotId, short TypeCode, string TypeName, int Quantity, DateTime MeasuredAt, string? Street, double Latitude, double Longitude);

/// <summary>Total = nombre de lignes groupées (base de la pagination). TotalObjects = somme des quantités.</summary>
public record PagedMeasurementsResponse(int Total, int TotalObjects, int Page, int PageSize, IReadOnlyList<MeasurementDto> Items);

public record MeasurementTypeBreakdownDto(short TypeCode, string TypeName, int Count);

/// <summary>Point géolocalisé pour la carte de concentration (onglet Détails) : un point par (snapshot, type détecté).</summary>
public record MeasurementPointDto(double Latitude, double Longitude, short TypeCode, string TypeName, int Quantity, string? Street);
