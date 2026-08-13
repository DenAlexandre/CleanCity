namespace CortexiaAuth.Api.Services;

/// <summary>
/// Tâches serveur unitaires (synchronisation Cortexia, calculs) — utilisées à la fois par le cycle
/// périodique (CortexiaImportBackgroundService) et par les déclenchements manuels (page Système).
/// </summary>
public interface IServerTaskService
{
    Task<int> ImportEdgesAndPlacesAsync(CancellationToken cancellationToken);

    Task<ImportMeasurementsResult> ImportMeasurementsAsync(CancellationToken cancellationToken);

    Task<int> CleanupDuplicateMeasurementsAsync(CancellationToken cancellationToken);

    Task<int> AssignItineraryNumbersAsync(CancellationToken cancellationToken);

    Task<int> DetectAlarmsAsync(CancellationToken cancellationToken);

    /// <summary>Récupère depuis Cortexia les relevés et notes Cci bruts (JSON) depuis une date donnée jusqu'à maintenant, sans les importer.</summary>
    Task<CortexiaDailyDataResult> DownloadDailyDataAsync(DateOnly date, CancellationToken cancellationToken);
}

public record ImportMeasurementsResult(int SnapshotsImported, int CciMeasurementsImported);

public record CortexiaDailyDataResult(byte[] SnapshotsJson, byte[] CciMeasurementsJson);
