using CortexiaAuth.Api.Data;
using CortexiaAuth.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CortexiaAuth.Api.Controllers;

/// <summary>
/// Destinataires des e-mails d'alarme (voir AlarmThreshold.SendEmail). Réservé à ManageAccounts,
/// même mécanisme d'authentification que les autres contrôleurs admin.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AlarmEmailRecipientsController(AppDbContext dbContext, PasswordHasher<AppUser> passwordHasher) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AlarmEmailRecipientDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<AlarmEmailRecipientDto>>> List(
        [FromHeader(Name = "X-Admin-Username")] string? adminUsername,
        [FromHeader(Name = "X-Admin-Password")] string? adminPassword,
        CancellationToken cancellationToken)
    {
        var authError = await AuthenticateAdminAsync(adminUsername, adminPassword, cancellationToken);
        if (authError is not null)
        {
            return authError;
        }

        var recipients = await dbContext.AlarmEmailRecipients.AsNoTracking().OrderBy(r => r.Email).ToListAsync(cancellationToken);
        return Ok(recipients.Select(ToDto));
    }

    [HttpPost]
    [ProducesResponseType(typeof(AlarmEmailRecipientDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AlarmEmailRecipientDto>> Create(
        [FromBody] SaveAlarmEmailRecipientRequest request,
        [FromHeader(Name = "X-Admin-Username")] string? adminUsername,
        [FromHeader(Name = "X-Admin-Password")] string? adminPassword,
        CancellationToken cancellationToken)
    {
        var authError = await AuthenticateAdminAsync(adminUsername, adminPassword, cancellationToken);
        if (authError is not null)
        {
            return authError;
        }

        if (await dbContext.AlarmEmailRecipients.AnyAsync(r => r.Email == request.Email, cancellationToken))
        {
            return Conflict(new { error = "Cette adresse e-mail est déjà destinataire." });
        }

        var recipient = new AlarmEmailRecipient { Email = request.Email };
        dbContext.AlarmEmailRecipients.Add(recipient);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(List), ToDto(recipient));
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

        var recipient = await dbContext.AlarmEmailRecipients.SingleOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (recipient is null)
        {
            return NotFound();
        }

        dbContext.AlarmEmailRecipients.Remove(recipient);
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private static AlarmEmailRecipientDto ToDto(AlarmEmailRecipient recipient) => new(recipient.Id, recipient.Email);

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
