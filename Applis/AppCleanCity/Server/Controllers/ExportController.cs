using System.Diagnostics;
using CortexiaAuth.Api.Data;
using CortexiaAuth.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CortexiaAuth.Api.Controllers;

/// <summary>
/// Export d'une sauvegarde .sql de la base, via "docker exec pg_dump" sur le conteneur Postgres
/// (voir start-postgres.ps1). Suppose que le conteneur tourne en local, ce qui est le cas de
/// l'environnement de développement actuel.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ExportController(AppDbContext dbContext, PasswordHasher<AppUser> passwordHasher, IConfiguration configuration) : ControllerBase
{
    [HttpGet("database")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ExportDatabase(
        [FromHeader(Name = "X-Admin-Username")] string? username,
        [FromHeader(Name = "X-Admin-Password")] string? password,
        CancellationToken cancellationToken)
    {
        var authError = await AuthenticateAsync(username, password, cancellationToken);
        if (authError is not null)
        {
            return authError;
        }

        var containerName = configuration["Export:PostgresContainerName"] ?? "cleancity-pg";
        var connectionString = new NpgsqlConnectionStringBuilder(configuration.GetConnectionString("Default"));

        var startInfo = new ProcessStartInfo("docker")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        startInfo.ArgumentList.Add("exec");
        startInfo.ArgumentList.Add(containerName);
        startInfo.ArgumentList.Add("pg_dump");
        startInfo.ArgumentList.Add("-U");
        startInfo.ArgumentList.Add(connectionString.Username ?? "postgres");
        startInfo.ArgumentList.Add("-d");
        startInfo.ArgumentList.Add(connectionString.Database ?? "cortexia_auth");
        startInfo.ArgumentList.Add("--no-owner");
        startInfo.ArgumentList.Add("--clean");
        startInfo.ArgumentList.Add("--if-exists");

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        await using var dump = new MemoryStream();
        var copyStdoutTask = process.StandardOutput.BaseStream.CopyToAsync(dump, cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await Task.WhenAll(copyStdoutTask, stderrTask);
        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                error = $"pg_dump a échoué (code {process.ExitCode}) : {stderrTask.Result}",
            });
        }

        var fileName = $"cortexia_auth_{DateTime.UtcNow:yyyyMMdd_HHmmss}.sql";
        return File(dump.ToArray(), "application/sql", fileName);
    }

    /// <summary>
    /// Authentifie l'appelant via les headers X-Admin-Username / X-Admin-Password et vérifie le
    /// droit ViewSysteme (même contrat que les autres contrôleurs, pas de session/JWT côté site).
    /// </summary>
    private async Task<ActionResult?> AuthenticateAsync(string? username, string? password, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            return Unauthorized(new { error = "Authentification requise pour exporter la base." });
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
