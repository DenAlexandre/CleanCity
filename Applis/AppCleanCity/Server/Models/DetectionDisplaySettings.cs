namespace CortexiaAuth.Api.Models;

/// <summary>
/// Réglages d'affichage des seuils de détection sur la carte (Mesures). Table singleton : une
/// seule ligne (Id = 1), créée avec les valeurs par défaut au premier accès si absente.
/// </summary>
public class DetectionDisplaySettings
{
    public int Id { get; set; }

    public double PositiveMin { get; set; } = 0;
    public double PositiveMax { get; set; } = 3.5;
    public string PositiveColor { get; set; } = "#e53935";

    public double AverageMin { get; set; } = 3.5;
    public double AverageMax { get; set; } = 4.2;
    public string AverageColor { get; set; } = "#fb8c00";

    /// <summary>Masque, dans l'onglet Détails de la page Mesures, les objets détectés sans rue associée.</summary>
    public bool HideObjectsWithoutStreet { get; set; }
}
