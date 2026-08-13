using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using CortexiaAuth.Api.Data;
using CortexiaAuth.Api.Models;
using CortexiaAuth.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CortexiaAuth.Api.Controllers;

/// <summary>
/// Sauvegarde/restauration de la base via pg_dump/psql, connectés directement à la base configurée
/// (ConnectionStrings:Default) — fonctionne aussi bien en local qu'en production (Neon), sans
/// dépendre d'un conteneur Postgres local.
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

        var connectionString = new NpgsqlConnectionStringBuilder(PostgresConnectionString.Normalize(configuration.GetConnectionString("Default")));

        var startInfo = NewPostgresProcessStartInfo("pg_dump", connectionString);
        startInfo.RedirectStandardOutput = true;
        startInfo.ArgumentList.Add("--no-owner");
        startInfo.ArgumentList.Add("--clean");
        startInfo.ArgumentList.Add("--if-exists");

        try
        {
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

            var sql = RemoveRedundantConstraintDrops(Encoding.UTF8.GetString(dump.ToArray()));

            // Compressé : un dump SQL brut (plein de DROP/ALTER/INSERT) déclenche le WAF de Cloudflare
            // devant Render ("Blocked") dès qu'on le réutilise pour restaurer — le contenu compressé
            // n'est plus lisible en clair par l'inspection de contenu. Réduit aussi la taille du fichier.
            await using var gzip = new MemoryStream();
            await using (var gzipStream = new GZipStream(gzip, CompressionLevel.Optimal, leaveOpen: true))
            {
                await gzipStream.WriteAsync(Encoding.UTF8.GetBytes(sql), cancellationToken);
            }

            var fileName = $"cortexia_auth_{DateTime.UtcNow:yyyyMMdd_HHmmss}.sql.gz";
            return File(gzip.ToArray(), "application/gzip", fileName);
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                error = $"Impossible de lancer pg_dump ({ex.Message}) : vérifiez que le paquet postgresql-client est installé dans l'image.",
            });
        }
    }

    /// <summary>
    /// Restaure la base à partir d'un fichier .sql.gz généré par ExportDatabase : ce fichier contient déjà
    /// des "DROP ... IF EXISTS" (option --clean du pg_dump), donc son exécution efface et recrée les
    /// objets existants — destructeur et irréversible, à n'utiliser qu'en connaissance de cause.
    /// </summary>
    [HttpPost("restore")]
    [RequestSizeLimit(500_000_000)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RestoreDatabase(
        IFormFile file,
        [FromHeader(Name = "X-Admin-Username")] string? username,
        [FromHeader(Name = "X-Admin-Password")] string? password,
        CancellationToken cancellationToken)
    {
        var authError = await AuthenticateAsync(username, password, cancellationToken);
        if (authError is not null)
        {
            return authError;
        }

        if (file is null || file.Length == 0)
        {
            return BadRequest(new { error = "Fichier vide ou manquant." });
        }

        if (!string.Equals(Path.GetExtension(file.FileName), ".gz", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { error = "Seul un fichier .sql.gz (généré par \"Exporter la base de données\") est accepté." });
        }

        var connectionString = new NpgsqlConnectionStringBuilder(PostgresConnectionString.Normalize(configuration.GetConnectionString("Default")));

        var startInfo = NewPostgresProcessStartInfo("psql", connectionString);
        startInfo.RedirectStandardInput = true;
        startInfo.ArgumentList.Add("--single-transaction");
        startInfo.ArgumentList.Add("-v");
        startInfo.ArgumentList.Add("ON_ERROR_STOP=1");

        try
        {
            using var process = new Process { StartInfo = startInfo };
            process.Start();

            await using (var fileStream = file.OpenReadStream())
            await using (var gzipStream = new GZipStream(fileStream, CompressionMode.Decompress))
            {
                await gzipStream.CopyToAsync(process.StandardInput.BaseStream, cancellationToken);
            }
            process.StandardInput.Close();

            var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    error = $"La restauration a échoué (code {process.ExitCode}) : {await stderrTask}",
                });
            }

            return NoContent();
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                error = $"Impossible de lancer psql ({ex.Message}) : vérifiez que le paquet postgresql-client est installé dans l'image.",
            });
        }
    }

    private static readonly Regex AttachPartitionRegex = new("ATTACH PARTITION public\\.\"([^\"]+)\"", RegexOptions.Multiline);
    private static readonly Regex DropConstraintRegex = new(
        """^ALTER TABLE IF EXISTS ONLY public\."[^"]+" DROP CONSTRAINT IF EXISTS "([^"]+)";\r?\n""", RegexOptions.Multiline);

    /// <summary>
    /// pg_dump --clean émet, pour les tables partitionnées (EdgeSnapshots/EdgeCciMeasurements), un
    /// "DROP CONSTRAINT" sur la clé primaire de chaque partition — or Postgres refuse de dropper
    /// directement une contrainte rattachée à l'index du parent ("cannot drop inherited constraint").
    /// Ce drop est de toute façon redondant : la table elle-même est entièrement supprimée quelques
    /// lignes plus loin (ce qui supprime sa contrainte avec elle). On ne retire que ces contraintes
    /// rattachées (identifiées via "ATTACH PARTITION") — les autres DROP CONSTRAINT restent, ils
    /// assurent l'ordre correct des drops entre tables lorsqu'il y a des clés étrangères.
    /// </summary>
    private static string RemoveRedundantConstraintDrops(string sql)
    {
        var attachedNames = AttachPartitionRegex.Matches(sql).Select(m => m.Groups[1].Value).ToHashSet();
        return DropConstraintRegex.Replace(sql, m => attachedNames.Contains(m.Groups[1].Value) ? "" : m.Value);
    }

    private static ProcessStartInfo NewPostgresProcessStartInfo(string executable, NpgsqlConnectionStringBuilder connectionString)
    {
        var startInfo = new ProcessStartInfo(executable)
        {
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        startInfo.ArgumentList.Add("-h");
        startInfo.ArgumentList.Add(connectionString.Host ?? "localhost");
        startInfo.ArgumentList.Add("-p");
        startInfo.ArgumentList.Add((connectionString.Port == 0 ? 5432 : connectionString.Port).ToString());
        startInfo.ArgumentList.Add("-U");
        startInfo.ArgumentList.Add(connectionString.Username ?? "postgres");
        startInfo.ArgumentList.Add("-d");
        startInfo.ArgumentList.Add(connectionString.Database ?? "cortexia_auth");

        // PGPASSWORD (plutôt qu'un mot de passe dans les arguments) : invisible depuis "ps"/la liste des
        // process d'autres utilisateurs. PGSSLMODE reprend le mode déjà négocié par Npgsql (Neon exige SSL).
        startInfo.Environment["PGPASSWORD"] = connectionString.Password ?? "";
        startInfo.Environment["PGSSLMODE"] = connectionString.SslMode switch
        {
            SslMode.Disable => "disable",
            SslMode.Allow => "allow",
            SslMode.Prefer => "prefer",
            SslMode.Require => "require",
            SslMode.VerifyCA => "verify-ca",
            SslMode.VerifyFull => "verify-full",
            _ => "prefer",
        };

        return startInfo;
    }

    /// <summary>
    /// Authentifie l'appelant via les headers X-Admin-Username / X-Admin-Password et vérifie le
    /// droit ViewSysteme (même contrat que les autres contrôleurs, pas de session/JWT côté site).
    /// </summary>
    private async Task<ActionResult?> AuthenticateAsync(string? username, string? password, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            return Unauthorized(new { error = "Authentification requise pour exporter/restaurer la base." });
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
