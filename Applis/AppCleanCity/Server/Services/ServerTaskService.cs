using CortexiaAuth.Api.Data;
using CortexiaAuth.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CortexiaAuth.Api.Services;

/// <summary>
/// S'authentifie auprès de Cortexia avec le compte de service configuré (Import:ServiceAccountUsername),
/// comme le cycle périodique : un déclenchement manuel n'a donc pas besoin des identifiants Cortexia de
/// l'admin actuellement connecté au site.
/// </summary>
public class ServerTaskService(
    AppDbContext dbContext,
    IConfiguration configuration,
    ICortexiaGeoService geoService,
    ICortexiaAuthService cortexiaAuthService,
    ICortexiaCredentialProtector credentialProtector,
    IDataImportService importService,
    IAlarmDetectionService alarmDetectionService,
    ILogger<ServerTaskService> logger) : IServerTaskService
{
    private const string MeasurementsDataset = "measurements";

    public async Task<int> ImportEdgesAndPlacesAsync(CancellationToken cancellationToken)
    {
        var authorizationHeader = await AuthenticateServiceAccountAsync(cancellationToken);

        var result = await geoService.GetEdgesAndPlacesGeoJsonAsync(authorizationHeader, cancellationToken);
        EnsureSuccess(result);

        using var stream = new MemoryStream(result.Body);
        var imported = await importService.ImportRoadEdgesAsync(stream, cancellationToken);
        return imported.RowCount;
    }

    public async Task<ImportMeasurementsResult> ImportMeasurementsAsync(CancellationToken cancellationToken)
    {
        var authorizationHeader = await AuthenticateServiceAccountAsync(cancellationToken);

        var end = DateTime.UtcNow;
        var start = await GetCheckpointAsync(cancellationToken) ?? end.AddDays(-1);

        var snapshots = await geoService.GetAggregatedSnapshotsAsync(start, end, authorizationHeader, cancellationToken);
        EnsureSuccess(snapshots);
        int snapshotsImported;
        using (var stream = new MemoryStream(snapshots.Body))
        {
            snapshotsImported = (await importService.ImportSnapshotsAsync(stream, cancellationToken)).RowCount;
        }

        var cci = await geoService.GetEdgesAndPlacesCciAsync(start, end, authorizationHeader, cancellationToken);
        EnsureSuccess(cci);
        int cciImported;
        using (var stream = new MemoryStream(cci.Body))
        {
            cciImported = (await importService.ImportCciMeasurementsAsync(stream, cancellationToken)).RowCount;
        }

        await SetCheckpointAsync(end, cancellationToken);

        return new ImportMeasurementsResult(snapshotsImported, cciImported);
    }

    public Task<int> CleanupDuplicateMeasurementsAsync(CancellationToken cancellationToken) =>
        importService.CleanupDuplicateMeasurementsAsync(cancellationToken);

    public Task<int> AssignItineraryNumbersAsync(CancellationToken cancellationToken) =>
        importService.AssignItineraryNumbersAsync(cancellationToken);

    public Task<int> DetectAlarmsAsync(CancellationToken cancellationToken) =>
        alarmDetectionService.DetectAndNotifyAsync(cancellationToken);

    public async Task<CortexiaDailyDataResult> DownloadDailyDataAsync(DateOnly date, CancellationToken cancellationToken)
    {
        var authorizationHeader = await AuthenticateServiceAccountAsync(cancellationToken);

        // Comme le cycle périodique (ImportMeasurementsAsync) : la borne de fin est toujours "maintenant",
        // la date choisie n'est que le point de départ — Cortexia attend un intervalle [start, now).
        var start = date.ToDateTime(TimeOnly.MinValue);
        var end = DateTime.UtcNow;

        var snapshots = await geoService.GetAggregatedSnapshotsAsync(start, end, authorizationHeader, cancellationToken);
        EnsureSuccess(snapshots);

        var cci = await geoService.GetEdgesAndPlacesCciAsync(start, end, authorizationHeader, cancellationToken);
        EnsureSuccess(cci);

        return new CortexiaDailyDataResult(snapshots.Body, cci.Body);
    }

    private async Task<string> AuthenticateServiceAccountAsync(CancellationToken cancellationToken)
    {
        var username = configuration["Import:ServiceAccountUsername"];
        if (string.IsNullOrEmpty(username))
        {
            throw new InvalidOperationException("Aucun compte de service Cortexia configuré (Import:ServiceAccountUsername).");
        }

        var account = await dbContext.AppUsers.AsNoTracking().SingleOrDefaultAsync(u => u.Username == username, cancellationToken);
        if (account is null)
        {
            throw new InvalidOperationException($"Compte de service Cortexia '{username}' introuvable.");
        }

        var cortexiaPassword = credentialProtector.Unprotect(account.CortexiaPasswordProtected);
        var token = await cortexiaAuthService.GetAccessTokenAsync(account.CortexiaUsername, cortexiaPassword, cancellationToken);
        return $"{token.TokenType} {token.AccessToken}";
    }

    private static void EnsureSuccess(CortexiaProxyResult result)
    {
        if ((int)result.StatusCode is < 200 or > 299)
        {
            throw new InvalidOperationException($"Cortexia a répondu {(int)result.StatusCode}.");
        }
    }

    private async Task<DateTime?> GetCheckpointAsync(CancellationToken cancellationToken)
    {
        var checkpoint = await dbContext.ImportCheckpoints.AsNoTracking()
            .SingleOrDefaultAsync(c => c.Dataset == MeasurementsDataset, cancellationToken);
        return checkpoint is null ? null : DateTime.SpecifyKind(checkpoint.LastImportedUntilUtc, DateTimeKind.Utc);
    }

    private async Task SetCheckpointAsync(DateTime until, CancellationToken cancellationToken)
    {
        var checkpoint = await dbContext.ImportCheckpoints.SingleOrDefaultAsync(c => c.Dataset == MeasurementsDataset, cancellationToken);
        if (checkpoint is null)
        {
            dbContext.ImportCheckpoints.Add(new ImportCheckpoint { Dataset = MeasurementsDataset, LastImportedUntilUtc = until });
        }
        else
        {
            checkpoint.LastImportedUntilUtc = until;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Checkpoint '{Dataset}' avancé à {Until:u}.", MeasurementsDataset, until);
    }
}
