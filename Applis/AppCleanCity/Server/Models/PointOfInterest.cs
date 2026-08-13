using NetTopologySuite.Geometries;

namespace CortexiaAuth.Api.Models;

/// <summary>
/// Point d'intérêt saisi par un administrateur du site (à ne pas confondre avec
/// <see cref="Place"/>, importé automatiquement depuis Cortexia).
/// </summary>
public class PointOfInterest
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>Catégorie libre (ex: "Gares", "Écoles", "Parcs et squares"), utilisée pour le regroupement sur la page d'accueil.</summary>
    public string Category { get; set; } = string.Empty;

    public Point Location { get; set; } = null!;
    public DateTime CreatedAtUtc { get; set; }
}
