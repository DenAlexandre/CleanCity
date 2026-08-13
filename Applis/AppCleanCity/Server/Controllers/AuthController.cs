using CortexiaAuth.Api.Data;
using CortexiaAuth.Api.Models;
using CortexiaAuth.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CortexiaAuth.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(
    ICortexiaAuthService cortexiaAuthService,
    AppDbContext dbContext,
    PasswordHasher<AppUser> passwordHasher,
    ICortexiaCredentialProtector credentialProtector) : ControllerBase
{
    /// <summary>
    /// Récupère un access token auprès de Cortexia (login/access-token) et l'enregistre en base.
    /// Utilise directement des identifiants Cortexia (utile pour les tests, Swagger...).
    /// </summary>
    [HttpPost("token")]
    [ProducesResponseType(typeof(CortexiaTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CortexiaTokenResponse>> GetToken([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var token = await ExchangeAndPersistTokenAsync(request.Username, request.Password, request.Username, cancellationToken);
            return Ok(token);
        }
        catch (CortexiaAuthException ex)
        {
            return StatusCode((int)ex.StatusCode, new { error = ex.ResponseBody });
        }
    }

    /// <summary>
    /// Connexion "site" : l'utilisateur s'authentifie avec son propre identifiant/mot de passe.
    /// Le serveur retrouve les identifiants Cortexia associés et échange le token à sa place —
    /// le front n'a donc jamais besoin des vrais identifiants Cortexia.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(SiteLoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<SiteLoginResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await dbContext.AppUsers.Include(u => u.Role).SingleOrDefaultAsync(u => u.Username == request.Username, cancellationToken);
        if (user is null || passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new { error = "Identifiant ou mot de passe incorrect." });
        }

        var cortexiaPassword = credentialProtector.Unprotect(user.CortexiaPasswordProtected);

        try
        {
            var token = await ExchangeAndPersistTokenAsync(user.CortexiaUsername, cortexiaPassword, user.Username, cancellationToken);
            return Ok(new SiteLoginResponse(token.AccessToken, token.TokenType, user.Role.Permissions));
        }
        catch (CortexiaAuthException ex)
        {
            return StatusCode((int)ex.StatusCode, new { error = ex.ResponseBody });
        }
    }

    /// <summary>
    /// Crée un compte "site" relié à des identifiants Cortexia. Le tout premier compte du site
    /// (bootstrap, table vide) est créé librement avec accès complet. Ensuite, toute création
    /// requiert l'authentification d'un compte ayant le droit ManageAccounts.
    /// </summary>
    [HttpPost("users")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegisterUser(
        [FromBody] RegisterAppUserRequest request,
        [FromHeader(Name = "X-Admin-Username")] string? adminUsername,
        [FromHeader(Name = "X-Admin-Password")] string? adminPassword,
        CancellationToken cancellationToken)
    {
        var isBootstrap = !await dbContext.AppUsers.AnyAsync(cancellationToken);
        int roleId;

        if (isBootstrap)
        {
            var adminRole = await dbContext.Roles.SingleOrDefaultAsync(r => r.Name == "Admin", cancellationToken);
            if (adminRole is null)
            {
                adminRole = new Role { Name = "Admin", Permissions = UserPermissions.FullAccess() };
                dbContext.Roles.Add(adminRole);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            roleId = adminRole.Id;
        }
        else
        {
            var (_, authError) = await AuthenticateAdminAsync(adminUsername, adminPassword, cancellationToken);
            if (authError is not null)
            {
                return authError;
            }

            if (request.RoleId is null || !await dbContext.Roles.AnyAsync(r => r.Id == request.RoleId, cancellationToken))
            {
                return BadRequest(new { error = "Rôle inconnu : renseignez un RoleId valide." });
            }

            roleId = request.RoleId.Value;
        }

        if (await dbContext.AppUsers.AnyAsync(u => u.Username == request.Username, cancellationToken))
        {
            return Conflict(new { error = "Cet identifiant existe déjà." });
        }

        var user = new AppUser
        {
            Username = request.Username,
            Email = request.Email,
            CortexiaUsername = request.CortexiaUsername,
            CortexiaPasswordProtected = credentialProtector.Protect(request.CortexiaPassword),
            RoleId = roleId,
            CreatedAtUtc = DateTime.UtcNow,
        };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        dbContext.AppUsers.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(ListUsers), new { username = user.Username });
    }

    /// <summary>Liste des comptes du site (sans mots de passe). Réservé à ManageAccounts.</summary>
    [HttpGet("users")]
    [ProducesResponseType(typeof(List<AppUserSummary>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ListUsers(
        [FromHeader(Name = "X-Admin-Username")] string? adminUsername,
        [FromHeader(Name = "X-Admin-Password")] string? adminPassword,
        CancellationToken cancellationToken)
    {
        var (_, authError) = await AuthenticateAdminAsync(adminUsername, adminPassword, cancellationToken);
        if (authError is not null)
        {
            return authError;
        }

        var users = await dbContext.AppUsers
            .AsNoTracking()
            .Include(u => u.Role)
            .OrderBy(u => u.Username)
            .Select(u => new AppUserSummary(u.Username, u.Email, u.CortexiaUsername, u.RoleId, u.Role.Name, u.Role.Permissions, u.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return Ok(users);
    }

    /// <summary>
    /// Modifie un compte (identifiant, email, identifiants Cortexia, rôle). Réservé à ManageAccounts.
    /// Le mot de passe Cortexia n'est mis à jour que si un nouveau est fourni (jamais renvoyé au
    /// front, donc pas possible de le pré-remplir dans un formulaire d'édition).
    /// </summary>
    [HttpPut("users/{username}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateAccount(
        string username,
        [FromBody] UpdateAccountRequest request,
        [FromHeader(Name = "X-Admin-Username")] string? adminUsername,
        [FromHeader(Name = "X-Admin-Password")] string? adminPassword,
        CancellationToken cancellationToken)
    {
        var (_, authError) = await AuthenticateAdminAsync(adminUsername, adminPassword, cancellationToken);
        if (authError is not null)
        {
            return authError;
        }

        var user = await dbContext.AppUsers.SingleOrDefaultAsync(u => u.Username == username, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        if (!await dbContext.Roles.AnyAsync(r => r.Id == request.RoleId, cancellationToken))
        {
            return BadRequest(new { error = "Rôle inconnu." });
        }

        if (!string.Equals(user.Username, request.Username, StringComparison.Ordinal)
            && await dbContext.AppUsers.AnyAsync(u => u.Username == request.Username, cancellationToken))
        {
            return Conflict(new { error = "Cet identifiant existe déjà." });
        }

        user.Username = request.Username;
        user.Email = request.Email;
        user.CortexiaUsername = request.CortexiaUsername;
        if (!string.IsNullOrEmpty(request.CortexiaPassword))
        {
            user.CortexiaPasswordProtected = credentialProtector.Protect(request.CortexiaPassword);
        }
        user.RoleId = request.RoleId;

        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    /// <summary>Réinitialise le mot de passe "site" d'un compte. Réservé à ManageAccounts.</summary>
    [HttpPost("users/{username}/reset-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetPassword(
        string username,
        [FromBody] ResetPasswordRequest request,
        [FromHeader(Name = "X-Admin-Username")] string? adminUsername,
        [FromHeader(Name = "X-Admin-Password")] string? adminPassword,
        CancellationToken cancellationToken)
    {
        var (_, authError) = await AuthenticateAdminAsync(adminUsername, adminPassword, cancellationToken);
        if (authError is not null)
        {
            return authError;
        }

        var user = await dbContext.AppUsers.SingleOrDefaultAsync(u => u.Username == username, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        user.PasswordHash = passwordHasher.HashPassword(user, request.NewPassword);
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    /// <summary>Supprime un compte. Réservé à ManageAccounts ; on ne peut pas se supprimer soi-même.</summary>
    [HttpDelete("users/{username}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser(
        string username,
        [FromHeader(Name = "X-Admin-Username")] string? adminUsername,
        [FromHeader(Name = "X-Admin-Password")] string? adminPassword,
        CancellationToken cancellationToken)
    {
        var (admin, authError) = await AuthenticateAdminAsync(adminUsername, adminPassword, cancellationToken);
        if (authError is not null)
        {
            return authError;
        }

        if (string.Equals(admin!.Username, username, StringComparison.Ordinal))
        {
            return BadRequest(new { error = "Impossible de supprimer votre propre compte." });
        }

        var user = await dbContext.AppUsers.SingleOrDefaultAsync(u => u.Username == username, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        dbContext.AppUsers.Remove(user);
        await dbContext.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Authentifie l'appelant comme administrateur via les headers X-Admin-Username / X-Admin-Password.
    /// Pas de session/JWT côté site : chaque action sensible re-vérifie les identifiants.
    /// </summary>
    private async Task<(AppUser? Admin, IActionResult? Error)> AuthenticateAdminAsync(
        string? adminUsername, string? adminPassword, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(adminUsername) || string.IsNullOrEmpty(adminPassword))
        {
            var accountCount = await dbContext.AppUsers.CountAsync(cancellationToken);
            var reason = accountCount == 0
                ? "Aucun compte n'existe encore : créez le premier sans renseigner ces champs, il obtiendra tous les droits automatiquement."
                : $"{accountCount} compte(s) existent déjà sur ce site, donc cette action nécessite de s'authentifier avec un compte administrateur existant.";

            return (null, Unauthorized(new
            {
                error = $"Authentification administrateur requise. {reason} " +
                        "Renseignez les champs 'X-Admin-Username' et 'X-Admin-Password' de cette requête " +
                        "(identifiant/mot de passe SITE, pas Cortexia, d'un compte ayant le droit 'Gestion des comptes').",
            }));
        }

        var admin = await dbContext.AppUsers.Include(u => u.Role).SingleOrDefaultAsync(u => u.Username == adminUsername, cancellationToken);
        if (admin is null || passwordHasher.VerifyHashedPassword(admin, admin.PasswordHash, adminPassword) == PasswordVerificationResult.Failed)
        {
            return (null, Unauthorized(new { error = $"Aucun compte administrateur ne correspond à l'identifiant '{adminUsername}' avec ce mot de passe." }));
        }

        if (!admin.Role.Permissions.ManageAccounts)
        {
            return (null, Unauthorized(new { error = $"Le compte '{adminUsername}' existe mais n'a pas le droit 'Gestion des comptes'." }));
        }

        return (admin, null);
    }

    private async Task<CortexiaTokenResponse> ExchangeAndPersistTokenAsync(
        string cortexiaUsername, string cortexiaPassword, string recordedUsername, CancellationToken cancellationToken)
    {
        var token = await cortexiaAuthService.GetAccessTokenAsync(cortexiaUsername, cortexiaPassword, cancellationToken);

        dbContext.AccessTokens.Add(new AccessTokenRecord
        {
            Username = recordedUsername,
            AccessToken = token.AccessToken,
            TokenType = token.TokenType,
            CreatedAtUtc = DateTime.UtcNow,
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        return token;
    }
}
