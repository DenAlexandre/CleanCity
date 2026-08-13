namespace CortexiaAuth.Api.Services;

/// <summary>
/// Rafraîchit périodiquement notre base à partir de Cortexia : le réseau routier/places
/// (peu volatile) et les mesures (snapshots, CCI) sur des intervalles séparés et configurables.
/// S'authentifie auprès de Cortexia avec les identifiants stockés du compte de service configuré
/// (Import:ServiceAccountUsername) — aucune session utilisateur n'est nécessaire.
/// </summary>
public class CortexiaImportBackgroundService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<CortexiaImportBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var edgesInterval = TimeSpan.FromHours(configuration.GetValue("Import:EdgesAndPlacesIntervalHours", 24));
        var measurementsInterval = TimeSpan.FromHours(configuration.GetValue("Import:MeasurementsIntervalHours", 1));

        await Task.WhenAll(
            RunLoopAsync("edges_and_places", edgesInterval, ImportEdgesAndPlacesAsync, stoppingToken),
            RunLoopAsync("measurements", measurementsInterval, ImportMeasurementsAsync, stoppingToken));
    }

    private async Task RunLoopAsync(string label, TimeSpan interval, Func<CancellationToken, Task> action, CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(interval);

        await RunOnceAsync(label, action, stoppingToken);
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunOnceAsync(label, action, stoppingToken);
        }
    }

    private async Task RunOnceAsync(string label, Func<CancellationToken, Task> action, CancellationToken stoppingToken)
    {
        try
        {
            logger.LogInformation("Import Cortexia [{Label}] : démarrage.", label);
            await action(stoppingToken);
            logger.LogInformation("Import Cortexia [{Label}] : terminé.", label);
        }
        catch (OperationCanceledException)
        {
            // Arrêt normal de l'application.
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Import Cortexia [{Label}] : échec, nouvelle tentative au prochain cycle.", label);
        }
    }

    private async Task ImportEdgesAndPlacesAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var taskService = scope.ServiceProvider.GetRequiredService<IServerTaskService>();
        var count = await taskService.ImportEdgesAndPlacesAsync(cancellationToken);
        logger.LogInformation("Import edges_and_places : {Count} features.", count);
    }

    private async Task ImportMeasurementsAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var taskService = scope.ServiceProvider.GetRequiredService<IServerTaskService>();

        var imported = await taskService.ImportMeasurementsAsync(cancellationToken);
        logger.LogInformation(
            "Import mesures : {Snapshots} relevé(s), {Cci} note(s) Cci.", imported.SnapshotsImported, imported.CciMeasurementsImported);

        // EdgeSnapshots/EdgeCciMeasurements sont alimentées en append-only (COPY sans upsert) :
        // un cycle rejoué après échec partiel (ex: checkpoint non avancé) peut réinsérer les mêmes
        // lignes. On nettoie systématiquement en fin de cycle plutôt que de laisser les doublons
        // s'accumuler.
        var deduplicated = await taskService.CleanupDuplicateMeasurementsAsync(cancellationToken);
        if (deduplicated > 0)
        {
            logger.LogInformation("Nettoyage mesures : {Count} doublon(s) supprimé(s).", deduplicated);
        }

        // Un import tardif de données plus anciennes que le dernier cycle changerait le premier
        // relevé du jour déjà utilisé pour découper les itinéraires : on recalcule systématiquement.
        var itinerariesUpdated = await taskService.AssignItineraryNumbersAsync(cancellationToken);
        if (itinerariesUpdated > 0)
        {
            logger.LogInformation("Itinéraires : {Count} numéro(s) recalculé(s).", itinerariesUpdated);
        }

        var newAlarms = await taskService.DetectAlarmsAsync(cancellationToken);
        if (newAlarms > 0)
        {
            logger.LogInformation("Alarmes : {Count} nouvelle(s) alarme(s) détectée(s).", newAlarms);
        }
    }
}
