namespace CortexiaAuth.Api.Models;

/// <summary>
/// Ville utilisée pour le bandeau météo. Table singleton : une seule ligne (Id = 1), créée avec
/// les valeurs par défaut (Palaiseau) au premier accès si absente.
/// </summary>
public class WeatherSettings
{
    public int Id { get; set; }
    public string City { get; set; } = "Palaiseau";
    public double Latitude { get; set; } = 48.7159;
    public double Longitude { get; set; } = 2.2465;
}
