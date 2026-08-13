using System.ComponentModel.DataAnnotations;

namespace CortexiaAuth.Api.Models;

public class UpdateAccountRequest
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string CortexiaUsername { get; set; } = string.Empty;

    /// <summary>Laisser vide pour conserver le mot de passe Cortexia existant.</summary>
    public string? CortexiaPassword { get; set; }

    [Required]
    public int RoleId { get; set; }
}
