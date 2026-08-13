using CortexiaAuth.Api.Data;
using CortexiaAuth.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace CortexiaAuth.Api.Controllers;

/// <summary>
/// Agrégats utilisés par la page d'accueil : note de propreté (Cci) et sa tendance, historique
/// hebdomadaire, rues les plus sales, note par catégorie de point d'intérêt (jointure spatiale
/// avec les tronçons/places à proximité) et alarmes récentes (objets sensibles détectés).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DashboardController(NpgsqlDataSource dataSource) : ControllerBase
{
    private const double DefaultPointOfInterestRadiusMeters = 500;

    /// <summary>Rayon configurable (page Paramètres, table PointOfInterestSettings) ; valeur par défaut si absente.</summary>
    private async Task<double> GetPointOfInterestRadiusMetersAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand("""SELECT "RadiusMeters" FROM "PointOfInterestSettings" LIMIT 1""", connection);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is double radius ? radius : DefaultPointOfInterestRadiusMeters;
    }

    [HttpGet("cleanliness-score")]
    [ProducesResponseType(typeof(CleanlinessScoreDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CleanlinessScoreDto>> GetCleanlinessScore(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        CancellationToken cancellationToken)
    {
        var duration = endDate - startDate;
        var previousStart = startDate - duration;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            SELECT
                (SELECT AVG("Cci") FROM "EdgeCciMeasurements" WHERE "MeasuredAt" >= @startDate AND "MeasuredAt" <= @endDate AND "Cci" IS NOT NULL) AS "CurrentAverage",
                (SELECT AVG("Cci") FROM "EdgeCciMeasurements" WHERE "MeasuredAt" >= @previousStart AND "MeasuredAt" < @startDate AND "Cci" IS NOT NULL) AS "PreviousAverage"
            """,
            connection);
        command.Parameters.AddWithValue("startDate", startDate);
        command.Parameters.AddWithValue("endDate", endDate);
        command.Parameters.AddWithValue("previousStart", previousStart);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);
        var current = reader.IsDBNull(0) ? (double?)null : reader.GetDouble(0);
        var previous = reader.IsDBNull(1) ? (double?)null : reader.GetDouble(1);

        return Ok(new CleanlinessScoreDto(current, previous));
    }

    [HttpGet("cleanliness-history")]
    [ProducesResponseType(typeof(IEnumerable<CleanlinessHistoryPointDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CleanlinessHistoryPointDto>>> GetCleanlinessHistory(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            SELECT date_trunc('week', "MeasuredAt")::date AS "WeekStart", AVG("Cci") AS "AverageCci"
            FROM "EdgeCciMeasurements"
            WHERE "MeasuredAt" >= @startDate AND "MeasuredAt" <= @endDate AND "Cci" IS NOT NULL
            GROUP BY "WeekStart"
            ORDER BY "WeekStart"
            """,
            connection);
        command.Parameters.AddWithValue("startDate", startDate);
        command.Parameters.AddWithValue("endDate", endDate);

        var items = new List<CleanlinessHistoryPointDto>();
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                items.Add(new CleanlinessHistoryPointDto(DateOnly.FromDateTime(reader.GetDateTime(0)), reader.GetDouble(1)));
            }
        }

        return Ok(items);
    }

    [HttpGet("dirtiest-streets")]
    [ProducesResponseType(typeof(IEnumerable<DirtiestStreetDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<DirtiestStreetDto>>> GetDirtiestStreets(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] int limit,
        CancellationToken cancellationToken)
    {
        limit = limit <= 0 ? 5 : Math.Min(limit, 50);

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            SELECT re."Name" AS "Street", AVG(c."Cci") AS "AverageCci"
            FROM "EdgeCciMeasurements" c
            JOIN "RoadEdges" re ON c."EdgeU" = re."U" AND c."EdgeV" = re."V" AND c."EdgeKey" = re."Key"
            WHERE c."MeasuredAt" >= @startDate AND c."MeasuredAt" <= @endDate AND c."Cci" IS NOT NULL AND re."Name" IS NOT NULL
            GROUP BY re."Name"
            ORDER BY AVG(c."Cci") ASC
            LIMIT @limit
            """,
            connection);
        command.Parameters.AddWithValue("startDate", startDate);
        command.Parameters.AddWithValue("endDate", endDate);
        command.Parameters.AddWithValue("limit", limit);

        var items = new List<DirtiestStreetDto>();
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                items.Add(new DirtiestStreetDto(reader.GetString(0), reader.GetDouble(1)));
            }
        }

        return Ok(items);
    }

    /// <summary>
    /// Note moyenne par catégorie de point d'intérêt : moyenne des Cci des tronçons/places situés
    /// à moins de 150m de chaque point (aucune note n'est directement associée à un POI).
    /// </summary>
    [HttpGet("points-of-interest-scores")]
    [ProducesResponseType(typeof(IEnumerable<PointOfInterestCategoryScoreDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<PointOfInterestCategoryScoreDto>>> GetPointOfInterestScores(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var radiusMeters = await GetPointOfInterestRadiusMetersAsync(connection, cancellationToken);

        await using var command = new NpgsqlCommand(
            """
            WITH poi_scores AS (
                SELECT
                    poi."Id",
                    poi."Category",
                    (
                        SELECT AVG(c."Cci")
                        FROM "EdgeCciMeasurements" c
                        LEFT JOIN "RoadEdges" re ON c."EdgeU" = re."U" AND c."EdgeV" = re."V" AND c."EdgeKey" = re."Key"
                        LEFT JOIN "Places" p ON c."PlaceId" = p."Id"
                        WHERE c."Cci" IS NOT NULL
                          AND c."MeasuredAt" >= @startDate AND c."MeasuredAt" <= @endDate
                          AND (
                            (re."Geometry" IS NOT NULL AND ST_DWithin(re."Geometry"::geography, poi."Location", @radius))
                            OR (p."Geometry" IS NOT NULL AND ST_DWithin(p."Geometry"::geography, poi."Location", @radius))
                          )
                    ) AS "PoiAverageCci"
                FROM "PointsOfInterest" poi
            )
            SELECT
                "Category",
                AVG("PoiAverageCci") AS "AverageCci",
                COUNT(*) AS "PoiCount"
            FROM poi_scores
            GROUP BY "Category"
            ORDER BY "Category"
            """,
            connection);
        command.Parameters.AddWithValue("startDate", startDate);
        command.Parameters.AddWithValue("endDate", endDate);
        command.Parameters.AddWithValue("radius", radiusMeters);

        var items = new List<PointOfInterestCategoryScoreDto>();
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                items.Add(new PointOfInterestCategoryScoreDto(
                    reader.GetString(0),
                    reader.IsDBNull(1) ? null : reader.GetDouble(1),
                    (int)reader.GetInt64(2)));
            }
        }

        return Ok(items);
    }

    /// <summary>
    /// Alarmes = relevés où le nombre d'objets d'un type détectés en un seul passage atteint ou
    /// dépasse le seuil configuré pour ce type (voir AlarmThresholdsController). Aucun seuil configuré = aucune alarme.
    /// </summary>
    [HttpGet("urgent-alerts")]
    [ProducesResponseType(typeof(IEnumerable<UrgentAlertDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<UrgentAlertDto>>> GetUrgentAlerts(
        [FromQuery] int limit,
        CancellationToken cancellationToken)
    {
        limit = limit <= 0 ? 5 : Math.Min(limit, 50);

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            WITH detected AS (
                SELECT s."Id", d.code AS "TypeCode", COUNT(*) AS "Count"
                FROM "EdgeSnapshots" s
                CROSS JOIN LATERAL unnest(s."Details") AS d(code)
                GROUP BY s."Id", d.code
            )
            SELECT s."MeasuredAt", COALESCE(re."Name", p."Name") AS "Street", detected."TypeCode", detected."Count", t."Quantity"
            FROM detected
            JOIN "AlarmThresholds" t ON t."TypeCode" = detected."TypeCode" AND detected."Count" >= t."Quantity"
            JOIN "EdgeSnapshots" s ON s."Id" = detected."Id"
            LEFT JOIN "RoadEdges" re ON s."EdgeU" = re."U" AND s."EdgeV" = re."V" AND s."EdgeKey" = re."Key"
            LEFT JOIN "Places" p ON s."PlaceId" = p."Id"
            ORDER BY s."MeasuredAt" DESC
            LIMIT @limit
            """,
            connection);
        command.Parameters.AddWithValue("limit", limit);

        var items = new List<UrgentAlertDto>();
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                var typeCode = reader.GetInt16(2);
                items.Add(new UrgentAlertDto(
                    reader.GetDateTime(0),
                    reader.IsDBNull(1) ? null : reader.GetString(1),
                    typeCode,
                    DetectionTypeCatalog.GetName(typeCode),
                    (int)reader.GetInt64(3),
                    reader.GetInt32(4)));
            }
        }

        return Ok(items);
    }
}
