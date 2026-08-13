namespace CortexiaAuth.Api.Models;

/// <summary>
/// Rôle assignable à un compte "site" : regroupe un jeu de droits (<see cref="UserPermissions"/>)
/// sous un nom géré dynamiquement (Admin, User, ...), au lieu de cases à cocher par compte.
/// </summary>
public class Role
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public UserPermissions Permissions { get; set; } = new();
}
