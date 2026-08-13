using System.Text.RegularExpressions;

namespace CortexiaAuth.Api.Services;

/// <summary>
/// Les identifiants d'edge Cortexia reprennent le format tuple OSMnx "(u, v, key)".
/// On les décompose pour stocker (U, V, Key) en colonnes typées plutôt qu'en texte répété
/// dans les tables de mesures à fort volume.
/// </summary>
public static partial class CortexiaIdParser
{
    [GeneratedRegex(@"^\(\s*(-?\d+)\s*,\s*(-?\d+)\s*,\s*(-?\d+)\s*\)$")]
    private static partial Regex EdgeIdPattern();

    public static (long U, long V, short Key) ParseEdgeId(string id)
    {
        if (!TryParseEdgeId(id, out var edge))
        {
            throw new FormatException($"Format d'id d'edge Cortexia inattendu : '{id}'.");
        }

        return edge;
    }

    /// <summary>
    /// Certains ids Cortexia (aggregated_snapshots, edges_and_places_cci) référencent une place
    /// (id Elasticsearch opaque) plutôt qu'un edge : on ne peut donc pas supposer que l'id soit
    /// toujours un tuple "(u, v, key)".
    /// </summary>
    public static bool TryParseEdgeId(string id, out (long U, long V, short Key) edge)
    {
        var match = EdgeIdPattern().Match(id);
        if (!match.Success)
        {
            edge = default;
            return false;
        }

        edge = (
            long.Parse(match.Groups[1].Value),
            long.Parse(match.Groups[2].Value),
            short.Parse(match.Groups[3].Value));
        return true;
    }
}
