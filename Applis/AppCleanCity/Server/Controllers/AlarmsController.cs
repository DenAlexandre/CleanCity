using CortexiaAuth.Api.Data;
using CortexiaAuth.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CortexiaAuth.Api.Controllers;

/// <summary>
/// Historique des alarmes persistées (voir IAlarmDetectionService). Lecture libre, comme les
/// autres endpoints d'affichage (Dashboard, Measurements). La purge (page Système) est en
/// revanche protégée, comme Export/Import.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AlarmsController(NpgsqlDataSource dataSource, AppDbContext dbContext, PasswordHasher<AppUser> passwordHasher) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedAlarmsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedAlarmsResponse>> List(
        [FromQuery] int page,
        [FromQuery] int pageSize,
        CancellationToken cancellationToken,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize <= 0 ? 20 : pageSize, 1, 200);

        const string sql = """
            SELECT a."Id", a."MeasuredAt", a."Street", a."TypeCode", a."Count", a."Threshold", a."EmailSent",
                   COUNT(*) OVER() AS "Total"
            FROM "Alarms" a
            WHERE (@startDate::timestamptz IS NULL OR a."MeasuredAt" >= @startDate)
              AND (@endDate::timestamptz IS NULL OR a."MeasuredAt" <= @endDate)
            ORDER BY a."MeasuredAt" DESC, a."Id" DESC
            LIMIT @pageSize OFFSET @offset
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("pageSize", pageSize);
        command.Parameters.AddWithValue("offset", (page - 1) * pageSize);
        command.Parameters.AddWithValue("startDate", (object?)startDate ?? DBNull.Value);
        command.Parameters.AddWithValue("endDate", (object?)endDate ?? DBNull.Value);

        var items = new List<AlarmDto>();
        var total = 0;
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                var typeCode = reader.GetInt16(3);
                items.Add(new AlarmDto(
                    reader.GetInt64(0),
                    reader.GetDateTime(1),
                    reader.IsDBNull(2) ? null : reader.GetString(2),
                    typeCode,
                    DetectionTypeCatalog.GetName(typeCode),
                    reader.GetInt32(4),
                    reader.GetInt32(5),
                    reader.GetBoolean(6)));
                total = reader.GetInt32(7);
            }
        }

        return Ok(new PagedAlarmsResponse(total, page, pageSize, items));
    }

    /// <summary>Vide entièrement l'historique des alarmes (page Système, utile après des tests de fausses alarmes).</summary>
    [HttpDelete]
    [ProducesResponseType(typeof(ClearAlarmsResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ClearAlarmsResult>> ClearAll(
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
        await using var command = new NpgsqlCommand("""DELETE FROM "Alarms" """, connection);
        var deletedCount = await command.ExecuteNonQueryAsync(cancellationToken);

        return Ok(new ClearAlarmsResult(deletedCount));
    }

    /// <summary>
    /// Authentifie l'appelant via les headers X-Admin-Username / X-Admin-Password et vérifie le
    /// droit ViewSysteme (même contrat qu'ExportController/ImportController : cette action est
    /// exposée depuis la page Système du site, pas de session/JWT côté site).
    /// </summary>
    private async Task<ActionResult?> AuthenticateAsync(string? username, string? password, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            return Unauthorized(new { error = "Authentification requise pour vider les alarmes." });
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

public record ClearAlarmsResult(int DeletedCount);
