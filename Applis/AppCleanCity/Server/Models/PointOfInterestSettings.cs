namespace CortexiaAuth.Api.Models;

/// <summary>
/// Rayon (mètres) utilisé pour calculer la note d'un point d'intérêt et le détail des objets
/// détectés à proximité. Table singleton : une seule ligne (Id = 1), créée avec la valeur par
/// défaut au premier accès si absente.
/// </summary>
public class PointOfInterestSettings
{
    public int Id { get; set; }
    public double RadiusMeters { get; set; } = 500;
}
