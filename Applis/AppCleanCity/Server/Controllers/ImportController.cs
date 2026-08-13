using CortexiaAuth.Api.Data;
using CortexiaAuth.Api.Models;
using CortexiaAuth.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CortexiaAuth.Api.Controllers;

/// <summary>
/// Import en base des exports Cortexia (edges_and_places.geojson, aggregated_snapshots, edges_and_places_cci).
/// Chargement par lot via COPY binaire Npgsql, adapté aux gros volumes.
/// </summary>
[ApiController]
[Route("api/import")]
[RequestSizeLimit(500_000_000)]
public class ImportController(
    IDataImportService importService,
    IAlarmDetectionService alarmDetectionService,
    AppDbContext dbContext,
    PasswordHasher<AppUser> passwordHasher,
    NpgsqlDataSource dataSource) : ControllerBase
{
    [HttpPost("road-edges")]
    [ProducesResponseType(typeof(ImportResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<ActionResult<ImportResult>> ImportRoadEdges(IFormFile file, CancellationToken cancellationToken) =>
        RunImportAsync(file, importService.ImportRoadEdgesAsync, cancellationToken);

    /// <summary>
    /// Import de snapshots (utilisé aussi bien pour rattraper des données Cortexia que pour charger
    /// des relevés de test) : déclenche la détection d'alarmes tout de suite après, plutôt que
    /// d'attendre le prochain cycle horaire d'AlarmDetectionService.
    /// </summary>
    [HttpPost("snapshots")]
    [ProducesResponseType(typeof(ImportSnapshotsResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ImportSnapshotsResult>> ImportSnapshots(
        IFormFile file,
        [FromHeader(Name = "X-Admin-Username")] string? adminUsername,
        [FromHeader(Name = "X-Admin-Password")] string? adminPassword,
        CancellationToken cancellationToken)
    {
        var authError = await AuthenticateAsync(adminUsername, adminPassword, cancellationToken);
        if (authError is not null)
        {
            return authError;
        }

        if (file is null || file.Length == 0)
        {
            return BadRequest(new { error = "Fichier vide ou manquant." });
        }

        if (!AllowedExtensions.Contains(Path.GetExtension(file.FileName), StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest(new { error = "Seuls les fichiers .json sont acceptés." });
        }

        try
        {
            ImportResult imported;
            await using (var stream = file.OpenReadStream())
            {
                imported = await importService.ImportSnapshotsAsync(stream, cancellationToken);
            }

            // Même ordre que le cycle d'import Cortexia en arrière-plan : sans cette déduplication,
            // charger deux fois le même fichier de test créerait deux relevés distincts et donc deux
            // alarmes pour ce qui est en réalité une seule et même détection.
            await importService.CleanupDuplicateMeasurementsAsync(cancellationToken);

            var alarmsCreated = await alarmDetectionService.DetectAndNotifyAsync(cancellationToken);
            return Ok(new ImportSnapshotsResult(imported.RowCount, alarmsCreated));
        }
        catch (ImportValidationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("cci-measurements")]
    [ProducesResponseType(typeof(ImportResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<ActionResult<ImportResult>> ImportCciMeasurements(IFormFile file, CancellationToken cancellationToken) =>
        RunImportAsync(file, importService.ImportCciMeasurementsAsync, cancellationToken);

    /// <summary>
    /// Supprime les données importées (relevés, notes Cci, alarmes) à partir d'une date/heure donnée
    /// (page Système, utile pour nettoyer des données de test). Le réseau routier/lieux (RoadEdges,
    /// Places) et le point de reprise de la synchronisation Cortexia (ImportCheckpoints) ne sont pas
    /// touchés : les données supprimées ici ne seront pas re-récupérées automatiquement.
    /// </summary>
    [HttpDelete("data")]
    [ProducesResponseType(typeof(ClearImportDataResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ClearImportDataResult>> ClearData(
        [FromQuery] DateTime fromDate,
        [FromHeader(Name = "X-Admin-Username")] string? adminUsername,
        [FromHeader(Name = "X-Admin-Password")] string? adminPassword,
        CancellationToken cancellationToken)
    {
        var authError = await AuthenticateAsync(adminUsername, adminPassword, cancellationToken);
        if (authError is not null)
        {
            return authError;
        }

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        async Task<int> DeleteFromAsync(string table)
        {
            await using var command = new NpgsqlCommand($"""DELETE FROM "{table}" WHERE "MeasuredAt" >= @fromDate""", connection);
            command.Parameters.AddWithValue("fromDate", fromDate);
            return await command.ExecuteNonQueryAsync(cancellationToken);
        }

        // Les alarmes d'abord : elles référencent des relevés qu'on va supprimer juste après.
        var alarmsDeleted = await DeleteFromAsync("Alarms");
        var snapshotsDeleted = await DeleteFromAsync("EdgeSnapshots");
        var cciDeleted = await DeleteFromAsync("EdgeCciMeasurements");

        return Ok(new ClearImportDataResult(alarmsDeleted, snapshotsDeleted, cciDeleted));
    }

    private static readonly string[] AllowedExtensions = [".json"];

    private async Task<ActionResult<ImportResult>> RunImportAsync(
        IFormFile file, Func<Stream, CancellationToken, Task<ImportResult>> import, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { error = "Fichier vide ou manquant." });
        }

        if (!AllowedExtensions.Contains(Path.GetExtension(file.FileName), StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest(new { error = "Seuls les fichiers .json sont acceptés." });
        }

        try
        {
            await using var stream = file.OpenReadStream();
            return Ok(await import(stream, cancellationToken));
        }
        catch (ImportValidationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Authentifie l'appelant via les headers X-Admin-Username / X-Admin-Password et vérifie le
    /// droit ViewSysteme (même contrat que ExportController : cette action est exposée depuis la
    /// page Export du site, pas de session/JWT côté site).
    /// </summary>
    private async Task<ActionResult?> AuthenticateAsync(string? username, string? password, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            return Unauthorized(new { error = "Authentification requise pour importer des relevés." });
        }

        var user = await dbContext.AppUsers.Include(u => u.Role).SingleOrDefaultAsync(u => u.Username == username, cancellationToken);
        if (user is null || passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password) == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new { error = $"Aucun compte ne correspond à l'identifiant '{username}' avec ce mot de passe." });
        }

        if (!user.Role.Permissions.ViewSysteme)
        {
            return Unauthorized(new { error = $"Le compte '{username}' n'a pas le droit 'Système'." });
        }

        return null;
    }
}

public record ImportSnapshotsResult(int RowCount, int AlarmsCreated);

public record ClearImportDataResult(int AlarmsDeleted, int SnapshotsDeleted, int CciMeasurementsDeleted);
