using System.ComponentModel.DataAnnotations;

namespace CortexiaAuth.Api.Models;

public class RegisterAppUserRequest
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string CortexiaUsername { get; set; } = string.Empty;

    [Required]
    public string CortexiaPassword { get; set; } = string.Empty;

    /// <summary>Ignoré pour le tout premier compte du site (bootstrap) : rôle Admin assigné automatiquement.</summary>
    public int? RoleId { get; set; }
}
