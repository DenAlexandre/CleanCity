using System.IO.Compression;
using CortexiaAuth.Api.Data;
using CortexiaAuth.Api.Models;
using CortexiaAuth.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CortexiaAuth.Api.Controllers;

/// <summary>
/// Déclenchement manuel des tâches serveur normalement exécutées automatiquement par
/// CortexiaImportBackgroundService (page Système, composant "Tâches") : utile pour forcer un
/// rafraîchissement immédiat sans attendre le prochain cycle périodique.
/// </summary>
[ApiController]
[Route("api/tasks")]
public class ServerTasksController(
    IServerTaskService taskService,
    AppDbContext dbContext,
    PasswordHasher<AppUser> passwordHasher) : ControllerBase
{
    [HttpPost("edges-and-places")]
    [ProducesResponseType(typeof(ServerTaskResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public Task<ActionResult<ServerTaskResult>> ImportEdgesAndPlaces(
        [FromHeader(Name = "X-Admin-Username")] string? adminUsername,
        [FromHeader(Name = "X-Admin-Password")] string? adminPassword,
        CancellationToken cancellationToken) =>
        RunAsync(adminUsername, adminPassword, async ct =>
        {
            var count = await taskService.ImportEdgesAndPlacesAsync(ct);
            return new ServerTaskResult($"{count} tronçon(s)/lieu(x) synchronisé(s) depuis Cortexia.");
        }, cancellationToken);

    [HttpPost("measurements")]
    [ProducesResponseType(typeof(ServerTaskResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public Task<ActionResult<ServerTaskResult>> ImportMeasurements(
        [FromHeader(Name = "X-Admin-Username")] string? adminUsername,
        [FromHeader(Name = "X-Admin-Password")] string? adminPassword,
        CancellationToken cancellationToken) =>
        RunAsync(adminUsername, adminPassword, async ct =>
        {
            var imported = await taskService.ImportMeasurementsAsync(ct);
            return new ServerTaskResult(
                $"{imported.SnapshotsImported} relevé(s) et {imported.CciMeasurementsImported} note(s) Cci importé(s) depuis Cortexia.");
        }, cancellationToken);

    [HttpPost("cleanup-duplicates")]
    [ProducesResponseType(typeof(ServerTaskResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<ActionResult<ServerTaskResult>> CleanupDuplicates(
        [FromHeader(Name = "X-Admin-Username")] string? adminUsername,
        [FromHeader(Name = "X-Admin-Password")] string? adminPassword,
        CancellationToken cancellationToken) =>
        RunAsync(adminUsername, adminPassword, async ct =>
        {
            var count = await taskService.CleanupDuplicateMeasurementsAsync(ct);
            return new ServerTaskResult($"{count} doublon(s) supprimé(s).");
        }, cancellationToken);

    [HttpPost("assign-itinerary-numbers")]
    [ProducesResponseType(typeof(ServerTaskResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<ActionResult<ServerTaskResult>> AssignItineraryNumbers(
        [FromHeader(Name = "X-Admin-Username")] string? adminUsername,
        [FromHeader(Name = "X-Admin-Password")] string? adminPassword,
        CancellationToken cancellationToken) =>
        RunAsync(adminUsername, adminPassword, async ct =>
        {
            var count = await taskService.AssignItineraryNumbersAsync(ct);
            return new ServerTaskResult($"{count} numéro(s) d'itinéraire recalculé(s).");
        }, cancellationToken);

    [HttpPost("detect-alarms")]
    [ProducesResponseType(typeof(ServerTaskResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<ActionResult<ServerTaskResult>> DetectAlarms(
        [FromHeader(Name = "X-Admin-Username")] string? adminUsername,
        [FromHeader(Name = "X-Admin-Password")] string? adminPassword,
        CancellationToken cancellationToken) =>
        RunAsync(adminUsername, adminPassword, async ct =>
        {
            var count = await taskService.DetectAlarmsAsync(ct);
            return new ServerTaskResult($"{count} nouvelle(s) alarme(s) détectée(s).");
        }, cancellationToken);

    /// <summary>Télécharge les relevés et notes Cci bruts (JSON) reçus de Cortexia depuis une date donnée jusqu'à maintenant, sans les importer.</summary>
    [HttpGet("download-cortexia-data")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> DownloadCortexiaData(
        [FromQuery] DateOnly date,
        [FromHeader(Name = "X-Admin-Username")] string? adminUsername,
        [FromHeader(Name = "X-Admin-Password")] string? adminPassword,
        CancellationToken cancellationToken)
    {
        var authError = await AuthenticateAsync(adminUsername, adminPassword, cancellationToken);
        if (authError is not null)
        {
            return authError;
        }

        try
        {
            var data = await taskService.DownloadDailyDataAsync(date, cancellationToken);

            using var zipStream = new MemoryStream();
            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
            {
                var snapshotsEntry = archive.CreateEntry("aggregated_snapshots.json", CompressionLevel.Fastest);
                await using (var entryStream = snapshotsEntry.Open())
                {
                    await entryStream.WriteAsync(data.SnapshotsJson, cancellationToken);
                }

                var cciEntry = archive.CreateEntry("edges_and_places_cci.json", CompressionLevel.Fastest);
                await using (var entryStream = cciEntry.Open())
                {
                    await entryStream.WriteAsync(data.CciMeasurementsJson, cancellationToken);
                }
            }

            return File(zipStream.ToArray(), "application/zip", $"cortexia_{date:yyyy-MM-dd}.zip");
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { error = ex.Message });
        }
    }

    private async Task<ActionResult<ServerTaskResult>> RunAsync(
        string? username, string? password, Func<CancellationToken, Task<ServerTaskResult>> action, CancellationToken cancellationToken)
    {
        var authError = await AuthenticateAsync(username, password, cancellationToken);
        if (authError is not null)
        {
            return authError;
        }

        try
        {
            return Ok(await action(cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { error = ex.Message });
        }
    }

    /// <summary>Même contrat que ImportController/ExportController : header-auth site, droit ViewSysteme.</summary>
    private async Task<ActionResult?> AuthenticateAsync(string? username, string? password, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            return Unauthorized(new { error = "Authentification requise pour exécuter une tâche serveur." });
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

public record ServerTaskResult(string Message);
