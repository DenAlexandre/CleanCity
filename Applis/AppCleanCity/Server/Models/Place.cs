using NetTopologySuite.Geometries;

namespace CortexiaAuth.Api.Models;

/// <summary>
/// Point d'intérêt (parc, bâtiment, etc.) issu de edges_and_places. Contrairement aux edges,
/// l'id est un identifiant Elasticsearch opaque et la géométrie est un Point ou un Polygon.
/// </summary>
public class Place
{
    public string Id { get; set; } = string.Empty;
    public string? Name { get; set; }
    public int CityId { get; set; }
    public Geometry Geometry { get; set; } = null!;
}
