namespace CortexiaAuth.Api.Models;

/// <summary>
/// Compte "site" : l'utilisateur se connecte avec son propre identifiant/mot de passe, jamais
/// avec ses identifiants Cortexia. Le mot de passe Cortexia est chiffré (Data Protection), pas
/// haché, car on doit pouvoir le déchiffrer pour l'envoyer à Cortexia à chaque connexion.
/// </summary>
public class AppUser
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string CortexiaUsername { get; set; } = string.Empty;
    public string CortexiaPasswordProtected { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;
    public DateTime CreatedAtUtc { get; set; }
}
