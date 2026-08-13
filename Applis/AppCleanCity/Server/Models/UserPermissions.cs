namespace CortexiaAuth.Api.Models;

/// <summary>
/// Droits granulaires d'un compte "site" : un booléen par section du menu, plus la capacité
/// de gérer les autres comptes. Stocké comme owned type EF (colonnes directement sur AppUsers).
/// </summary>
public class UserPermissions
{
    public bool ManageAccounts { get; set; }
    public bool ViewMesures { get; set; }
    public bool ViewListeMesures { get; set; }
    public bool ViewItineraires { get; set; }
    public bool ViewPointsInteret { get; set; }
    public bool ViewAlertes { get; set; }
    public bool ViewParametres { get; set; }
    public bool ViewSysteme { get; set; }
    public bool ManageCortexia { get; set; }

    public static UserPermissions FullAccess() => new()
    {
        ManageAccounts = true,
        ViewMesures = true,
        ViewListeMesures = true,
        ViewItineraires = true,
        ViewPointsInteret = true,
        ViewAlertes = true,
        ViewParametres = true,
        ViewSysteme = true,
        ManageCortexia = true,
    };
}
