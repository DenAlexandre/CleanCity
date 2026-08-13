using CortexiaAuth.Api.Data;
using CortexiaAuth.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CortexiaAuth.Api.Controllers;

/// <summary>
/// Seuils déclenchant une alarme (type d'objet + quantité détectée en un seul passage). Réservé
/// à ManageAccounts, même mécanisme d'authentification que les autres contrôleurs admin.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AlarmThresholdsController(AppDbContext dbContext, PasswordHasher<AppUser> passwordHasher) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AlarmThresholdDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<AlarmThresholdDto>>> List(
        [FromHeader(Name = "X-Admin-Username")] string? adminUsername,
        [FromHeader(Name = "X-Admin-Password")] string? adminPassword,
        CancellationToken cancellationToken)
    {
        var authError = await AuthenticateAdminAsync(adminUsername, adminPassword, cancellationToken);
        if (authError is not null)
        {
            return authError;
        }

        var thresholds = await dbContext.AlarmThresholds.AsNoTracking().OrderBy(t => t.TypeCode).ToListAsync(cancellationToken);
        return Ok(thresholds.Select(ToDto));
    }

    /// <summary>Catalogue complet des types d'objets connus, pour peupler le sélecteur du formulaire.</summary>
    [HttpGet("types")]
    [ProducesResponseType(typeof(IEnumerable<DetectionTypeDto>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<DetectionTypeDto>> ListTypes()
    {
        return Ok(DetectionTypeCatalog.Names.Select(kv => new DetectionTypeDto(kv.Key, kv.Value)).OrderBy(t => t.TypeName));
    }

    [HttpPost]
    [ProducesResponseType(typeof(AlarmThresholdDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AlarmThresholdDto>> Create(
        [FromBody] SaveAlarmThresholdRequest request,
        [FromHeader(Name = "X-Admin-Username")] string? adminUsername,
        [FromHeader(Name = "X-Admin-Password")] string? adminPassword,
        CancellationToken cancellationToken)
    {
        var authError = await AuthenticateAdminAsync(adminUsername, adminPassword, cancellationToken);
        if (authError is not null)
        {
            return authError;
        }

        if (await dbContext.AlarmThresholds.AnyAsync(t => t.TypeCode == request.TypeCode, cancellationToken))
        {
            return Conflict(new { error = "Un seuil existe déjà pour ce type." });
        }

        var threshold = new AlarmThreshold { TypeCode = request.TypeCode, Quantity = request.Quantity, SendEmail = request.SendEmail };
        dbContext.AlarmThresholds.Add(threshold);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(List), ToDto(threshold));
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] SaveAlarmThresholdRequest request,
        [FromHeader(Name = "X-Admin-Username")] string? adminUsername,
        [FromHeader(Name = "X-Admin-Password")] string? adminPassword,
        CancellationToken cancellationToken)
    {
        var authError = await AuthenticateAdminAsync(adminUsername, adminPassword, cancellationToken);
        if (authError is not null)
        {
            return authError;
        }

        var threshold = await dbContext.AlarmThresholds.SingleOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (threshold is null)
        {
            return NotFound();
        }

        if (await dbContext.AlarmThresholds.AnyAsync(t => t.Id != id && t.TypeCode == request.TypeCode, cancellationToken))
        {
            return Conflict(new { error = "Un seuil existe déjà pour ce type." });
        }

        threshold.TypeCode = request.TypeCode;
        threshold.Quantity = request.Quantity;
        threshold.SendEmail = request.SendEmail;
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        int id,
        [FromHeader(Name = "X-Admin-Username")] string? adminUsername,
        [FromHeader(Name = "X-Admin-Password")] string? adminPassword,
        CancellationToken cancellationToken)
    {
        var authError = await AuthenticateAdminAsync(adminUsername, adminPassword, cancellationToken);
        if (authError is not null)
        {
            return authError;
        }

        var threshold = await dbContext.AlarmThresholds.SingleOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (threshold is null)
        {
            return NotFound();
        }

        dbContext.AlarmThresholds.Remove(threshold);
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private static AlarmThresholdDto ToDto(AlarmThreshold threshold) =>
        new(threshold.Id, threshold.TypeCode, DetectionTypeCatalog.GetName(threshold.TypeCode), threshold.Quantity, threshold.SendEmail);

    /// <summary>
    /// Authentifie l'appelant comme administrateur via les headers X-Admin-Username / X-Admin-Password
    /// (même contrat que les autres contrôleurs : pas de session/JWT côté site).
    /// </summary>
    private async Task<ActionResult?> AuthenticateAdminAsync(string? adminUsername, string? adminPassword, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(adminUsername) || string.IsNullOrEmpty(adminPassword))
        {
            return Unauthorized(new { error = "Authentification administrateur requise (headers X-Admin-Username / X-Admin-Password)." });
        }

        var admin = await dbContext.AppUsers.Include(u => u.Role).SingleOrDefaultAsync(u => u.Username == adminUsername, cancellationToken);
        if (admin is null || passwordHasher.VerifyHashedPassword(admin, admin.PasswordHash, adminPassword) == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new { error = $"Aucun compte administrateur ne correspond à l'identifiant '{adminUsername}' avec ce mot de passe." });
        }

        if (!admin.Role.Permissions.ManageAccounts)
        {
            return Unauthorized(new { error = $"Le compte '{adminUsername}' existe mais n'a pas le droit 'Gestion des comptes'." });
        }

        return null;
    }
}
