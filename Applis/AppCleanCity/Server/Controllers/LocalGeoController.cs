using CortexiaAuth.Api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Features;

namespace CortexiaAuth.Api.Controllers;

/// <summary>
/// Sert la cartographie depuis notre propre base (tables RoadEdges/Places, importées via
/// /api/import) plutôt que d'appeler Cortexia en direct : pas de dépendance à un token Cortexia
/// valide, pas d'aller-retour réseau vers Cortexia à chaque affichage de la carte.
/// </summary>
[ApiController]
[Route("api/geo/local")]
public class LocalGeoController(AppDbContext dbContext) : ControllerBase
{
    [HttpGet("edges-and-places")]
    [ProducesResponseType(typeof(FeatureCollection), StatusCodes.Status200OK)]
    public async Task<ActionResult<FeatureCollection>> GetEdgesAndPlaces(CancellationToken cancellationToken)
    {
        var featureCollection = new FeatureCollection();

        var edges = await dbContext.RoadEdges.AsNoTracking().ToListAsync(cancellationToken);
        foreach (var edge in edges)
        {
            var attributes = new AttributesTable
            {
                { "id", $"({edge.U}, {edge.V}, {edge.Key})" },
                { "osmid", edge.OsmIds },
                { "highway", edge.Highway },
                { "name", edge.Name },
                { "length", edge.LengthMeters },
            };
            featureCollection.Add(new Feature(edge.Geometry, attributes));
        }

        var places = await dbContext.Places.AsNoTracking().ToListAsync(cancellationToken);
        foreach (var place in places)
        {
            var attributes = new AttributesTable
            {
                { "id", place.Id },
                { "name", place.Name },
                { "city_id", place.CityId },
            };
            featureCollection.Add(new Feature(place.Geometry, attributes));
        }

        return Ok(featureCollection);
    }

    /// <summary>
    /// Noms distincts de toutes les rues/lieux du réseau routier local (RoadEdges + Places),
    /// indépendamment de toute période : pour l'auto-complétion de la recherche de rue sur la carte.
    /// </summary>
    [HttpGet("streets")]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<string>>> GetStreetNames(CancellationToken cancellationToken)
    {
        var edgeNames = await dbContext.RoadEdges.AsNoTracking()
            .Where(re => re.Name != null)
            .Select(re => re.Name!)
            .Distinct()
            .ToListAsync(cancellationToken);

        var placeNames = await dbContext.Places.AsNoTracking()
            .Where(p => p.Name != null)
            .Select(p => p.Name!)
            .Distinct()
            .ToListAsync(cancellationToken);

        var names = edgeNames.Concat(placeNames).Distinct().OrderBy(n => n, StringComparer.OrdinalIgnoreCase);
        return Ok(names);
    }

    /// <summary>
    /// Géométrie de tous les tronçons/lieux portant exactement ce nom, pour permettre à la carte de
    /// zoomer sur une rue choisie dans la recherche (voir GetStreetNames).
    /// </summary>
    [HttpGet("street")]
    [ProducesResponseType(typeof(FeatureCollection), StatusCodes.Status200OK)]
    public async Task<ActionResult<FeatureCollection>> GetStreetGeometry([FromQuery] string name, CancellationToken cancellationToken)
    {
        var featureCollection = new FeatureCollection();

        var edges = await dbContext.RoadEdges.AsNoTracking().Where(re => re.Name == name).ToListAsync(cancellationToken);
        foreach (var edge in edges)
        {
            featureCollection.Add(new Feature(edge.Geometry, new AttributesTable { { "name", edge.Name } }));
        }

        var places = await dbContext.Places.AsNoTracking().Where(p => p.Name == name).ToListAsync(cancellationToken);
        foreach (var place in places)
        {
            featureCollection.Add(new Feature(place.Geometry, new AttributesTable { { "name", place.Name } }));
        }

        return Ok(featureCollection);
    }

    /// <summary>
    /// Tronçons effectivement parcourus par un itinéraire (au moins un relevé EdgeSnapshots avec
    /// un ItineraryNumber assigné) sur la période donnée. Contrairement à edges-and-places (tout
    /// le réseau routier), ne renvoie que les segments réellement empruntés.
    /// </summary>
    [HttpGet("itinerary-edges")]
    [ProducesResponseType(typeof(FeatureCollection), StatusCodes.Status200OK)]
    public async Task<ActionResult<FeatureCollection>> GetItineraryEdges(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        CancellationToken cancellationToken)
    {
        var edges = await dbContext.RoadEdges
            .AsNoTracking()
            .Where(re => dbContext.EdgeSnapshots.Any(s =>
                s.EdgeU == re.U && s.EdgeV == re.V && s.EdgeKey == re.Key &&
                s.ItineraryNumber != null &&
                s.MeasuredAt >= startDate && s.MeasuredAt <= endDate))
            .ToListAsync(cancellationToken);

        var featureCollection = new FeatureCollection();
        foreach (var edge in edges)
        {
            var attributes = new AttributesTable
            {
                { "id", $"({edge.U}, {edge.V}, {edge.Key})" },
                { "name", edge.Name },
            };
            featureCollection.Add(new Feature(edge.Geometry, attributes));
        }

        return Ok(featureCollection);
    }

    /// <summary>
    /// Note (Cci moyen) par tronçon sur la période donnée, pour la coloration des détections
    /// (positive/moyenne) sur la carte. Ne renvoie que les tronçons ayant au moins une mesure.
    /// </summary>
    [HttpGet("edge-scores")]
    [ProducesResponseType(typeof(FeatureCollection), StatusCodes.Status200OK)]
    public async Task<ActionResult<FeatureCollection>> GetEdgeScores(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        CancellationToken cancellationToken)
    {
        var scores = await dbContext.EdgeCciMeasurements
            .AsNoTracking()
            .Where(c => c.EdgeU != null && c.Cci != null && c.MeasuredAt >= startDate && c.MeasuredAt <= endDate)
            .GroupBy(c => new { c.EdgeU, c.EdgeV, c.EdgeKey })
            .Select(g => new { g.Key.EdgeU, g.Key.EdgeV, g.Key.EdgeKey, AverageCci = g.Average(c => c.Cci!.Value) })
            .ToListAsync(cancellationToken);

        var edgeKeys = scores.Select(s => (s.EdgeU!.Value, s.EdgeV!.Value, s.EdgeKey!.Value)).ToHashSet();
        var edges = await dbContext.RoadEdges.AsNoTracking().ToListAsync(cancellationToken);
        var edgesByKey = edges
            .Where(e => edgeKeys.Contains((e.U, e.V, e.Key)))
            .ToDictionary(e => (e.U, e.V, e.Key));

        var featureCollection = new FeatureCollection();
        foreach (var score in scores)
        {
            if (!edgesByKey.TryGetValue((score.EdgeU!.Value, score.EdgeV!.Value, score.EdgeKey!.Value), out var edge))
            {
                continue;
            }

            var attributes = new AttributesTable
            {
                { "id", $"({edge.U}, {edge.V}, {edge.Key})" },
                { "name", edge.Name },
                { "cci", score.AverageCci },
            };
            featureCollection.Add(new Feature(edge.Geometry, attributes));
        }

        return Ok(featureCollection);
    }
}
