using CortexiaAuth.Api.Data;
using CortexiaAuth.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CortexiaAuth.Api.Controllers;

/// <summary>
/// Gestion des rôles (Admin, User, ...) et de leurs droits. Réservé aux comptes ayant le droit
/// ManageAccounts, même mécanisme d'authentification que AuthController.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class RolesController(AppDbContext dbContext, PasswordHasher<AppUser> passwordHasher) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<RoleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<RoleDto>>> List(
        [FromHeader(Name = "X-Admin-Username")] string? adminUsername,
        [FromHeader(Name = "X-Admin-Password")] string? adminPassword,
        CancellationToken cancellationToken)
    {
        var authError = await AuthenticateAdminAsync(adminUsername, adminPassword, cancellationToken);
        if (authError is not null)
        {
            return authError;
        }

        var roles = await dbContext.Roles.AsNoTracking().OrderBy(r => r.Name).ToListAsync(cancellationToken);
        return Ok(roles.Select(ToDto));
    }

    [HttpPost]
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RoleDto>> Create(
        [FromBody] SaveRoleRequest request,
        [FromHeader(Name = "X-Admin-Username")] string? adminUsername,
        [FromHeader(Name = "X-Admin-Password")] string? adminPassword,
        CancellationToken cancellationToken)
    {
        var authError = await AuthenticateAdminAsync(adminUsername, adminPassword, cancellationToken);
        if (authError is not null)
        {
            return authError;
        }

        if (await dbContext.Roles.AnyAsync(r => r.Name == request.Name, cancellationToken))
        {
            return Conflict(new { error = "Un rôle avec ce nom existe déjà." });
        }

        var role = new Role { Name = request.Name, Permissions = request.Permissions };
        dbContext.Roles.Add(role);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(List), ToDto(role));
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] SaveRoleRequest request,
        [FromHeader(Name = "X-Admin-Username")] string? adminUsername,
        [FromHeader(Name = "X-Admin-Password")] string? adminPassword,
        CancellationToken cancellationToken)
    {
        var authError = await AuthenticateAdminAsync(adminUsername, adminPassword, cancellationToken);
        if (authError is not null)
        {
            return authError;
        }

        var role = await dbContext.Roles.SingleOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (role is null)
        {
            return NotFound();
        }

        if (await dbContext.Roles.AnyAsync(r => r.Id != id && r.Name == request.Name, cancellationToken))
        {
            return Conflict(new { error = "Un rôle avec ce nom existe déjà." });
        }

        role.Name = request.Name;
        role.Permissions = request.Permissions;
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
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

        var role = await dbContext.Roles.SingleOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (role is null)
        {
            return NotFound();
        }

        if (await dbContext.AppUsers.AnyAsync(u => u.RoleId == id, cancellationToken))
        {
            return Conflict(new { error = "Ce rôle est utilisé par au moins un compte, impossible de le supprimer." });
        }

        dbContext.Roles.Remove(role);
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private static RoleDto ToDto(Role role) => new(role.Id, role.Name, role.Permissions);

    /// <summary>
    /// Authentifie l'appelant comme administrateur via les headers X-Admin-Username / X-Admin-Password
    /// (même contrat que AuthController.AuthenticateAdminAsync : pas de session/JWT côté site).
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
