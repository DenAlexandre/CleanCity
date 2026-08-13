namespace CortexiaAuth.Api.Services;

public record ImportResult(int RowCount);

public interface IDataImportService
{
    Task<ImportResult> ImportRoadEdgesAsync(Stream geoJsonStream, CancellationToken cancellationToken);

    Task<ImportResult> ImportSnapshotsAsync(Stream jsonStream, CancellationToken cancellationToken);

    Task<ImportResult> ImportCciMeasurementsAsync(Stream jsonStream, CancellationToken cancellationToken);

    /// <summary>
    /// Supprime les doublons introduits par des imports partiellement échoués puis rejoués sur la
    /// même plage (EdgeSnapshots/EdgeCciMeasurements sont alimentées par COPY append-only, sans
    /// upsert). Garde la ligne la plus ancienne (Id le plus petit) de chaque groupe de doublons.
    /// </summary>
    Task<int> CleanupDuplicateMeasurementsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Recalcule entièrement EdgeSnapshots.ItineraryNumber : au sein d'une même journée pour une
    /// suitcase donnée, une nouvelle fenêtre d'itinéraire (moins de 7h) démarre dès qu'un relevé
    /// est à plus de 7h du début de la fenêtre en cours. Recalcul complet (pas incrémental) car un
    /// import tardif de données plus anciennes changerait le début de journée déjà utilisé.
    /// </summary>
    Task<int> AssignItineraryNumbersAsync(CancellationToken cancellationToken);
}
