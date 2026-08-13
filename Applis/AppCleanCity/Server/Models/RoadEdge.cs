using NetTopologySuite.Geometries;

namespace CortexiaAuth.Api.Models;

/// <summary>
/// Segment du graphe routier OSMnx (edges_and_places). Clé composite (U, V, Key) reprenant
/// le tuple d'identifiant Cortexia "(u, v, key)", pour éviter de dupliquer une clé texte
/// dans les tables de mesures à fort volume.
/// </summary>
public class RoadEdge
{
    public long U { get; set; }
    public long V { get; set; }
    public short Key { get; set; }

    public long[] OsmIds { get; set; } = [];
    public string? Highway { get; set; }
    public string? Name { get; set; }
    public double LengthMeters { get; set; }
    public Geometry Geometry { get; set; } = null!;

    /// <summary>Reste des propriétés GeoJSON non typées (oneway, lanes, maxspeed, bridge, etc.).</summary>
    public string PropertiesJson { get; set; } = "{}";
}
