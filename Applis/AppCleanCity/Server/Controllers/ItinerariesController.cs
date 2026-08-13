using CortexiaAuth.Api.Data;
using CortexiaAuth.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace CortexiaAuth.Api.Controllers;

/// <summary>
/// Liste des itinéraires (une tournée de suitcase de moins de 7h, voir
/// IDataImportService.AssignItineraryNumbersAsync) avec les rues parcourues, les objets détectés
/// et la note de propreté (Cci moyen) sur la fenêtre de temps de l'itinéraire.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ItinerariesController(NpgsqlDataSource dataSource) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ItineraryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ItineraryDto>>> List(
        CancellationToken cancellationToken,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        const string sql = """
            WITH itineraries AS (
                SELECT
                    s."SuitcaseId",
                    date_trunc('day', s."MeasuredAt")::date AS "Day",
                    s."ItineraryNumber",
                    MIN(s."MeasuredAt") AS "StartTime",
                    MAX(s."MeasuredAt") AS "EndTime",
                    COUNT(*) AS "ObjectCount",
                    array_agg(DISTINCT COALESCE(re."Name", p."Name")) FILTER (WHERE COALESCE(re."Name", p."Name") IS NOT NULL) AS "Streets"
                FROM "EdgeSnapshots" s
                CROSS JOIN LATERAL unnest(s."Details") AS d(code)
                LEFT JOIN "RoadEdges" re ON s."EdgeU" = re."U" AND s."EdgeV" = re."V" AND s."EdgeKey" = re."Key"
                LEFT JOIN "Places" p ON s."PlaceId" = p."Id"
                WHERE s."ItineraryNumber" IS NOT NULL
                  AND (@startDate::timestamptz IS NULL OR s."MeasuredAt" >= @startDate)
                  AND (@endDate::timestamptz IS NULL OR s."MeasuredAt" <= @endDate)
                GROUP BY s."SuitcaseId", "Day", s."ItineraryNumber"
            )
            SELECT
                i.*,
                cci."AverageCci"
            FROM itineraries i
            LEFT JOIN LATERAL (
                SELECT AVG(c."Cci") AS "AverageCci"
                FROM "EdgeCciMeasurements" c
                WHERE c."SuitcaseIds" @> ARRAY[i."SuitcaseId"]
                  AND c."MeasuredAt" BETWEEN i."StartTime" AND i."EndTime"
            ) cci ON true
            ORDER BY i."Day" DESC, i."SuitcaseId", i."ItineraryNumber"
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("startDate", (object?)startDate ?? DBNull.Value);
        command.Parameters.AddWithValue("endDate", (object?)endDate ?? DBNull.Value);

        var items = new List<ItineraryDto>();
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                var streetsOrdinal = reader.GetOrdinal("Streets");
                var streets = reader.IsDBNull(streetsOrdinal) ? [] : (string[])reader.GetValue(streetsOrdinal);
                var cciOrdinal = reader.GetOrdinal("AverageCci");

                items.Add(new ItineraryDto(
                    reader.GetString(reader.GetOrdinal("SuitcaseId")),
                    DateOnly.FromDateTime(reader.GetDateTime(reader.GetOrdinal("Day"))),
                    reader.GetInt32(reader.GetOrdinal("ItineraryNumber")),
                    reader.GetDateTime(reader.GetOrdinal("StartTime")),
                    reader.GetDateTime(reader.GetOrdinal("EndTime")),
                    (int)reader.GetInt64(reader.GetOrdinal("ObjectCount")),
                    streets,
                    reader.IsDBNull(cciOrdinal) ? null : reader.GetDouble(cciOrdinal)));
            }
        }

        return Ok(items);
    }

    /// <summary>
    /// Détail par tronçon (rue) pour un itinéraire précis : objets détectés sur ce tronçon pendant
    /// l'itinéraire, et note de propreté (Cci moyen) de ce tronçon ce jour-là.
    /// </summary>
    [HttpGet("streets")]
    [ProducesResponseType(typeof(IEnumerable<ItineraryStreetDetailDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ItineraryStreetDetailDto>>> GetStreetDetails(
        [FromQuery] string suitcaseId,
        [FromQuery] DateOnly day,
        [FromQuery] int itineraryIndex,
        CancellationToken cancellationToken)
    {
        var dayDate = day.ToDateTime(TimeOnly.MinValue);

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);

        // Objets détectés par tronçon et par type, pendant l'itinéraire.
        var segmentOrder = new List<(long? EdgeU, long? EdgeV, short? EdgeKey, string? PlaceId)>();
        var objectsBySegment = new Dictionary<(long?, long?, short?, string?), List<MeasurementTypeBreakdownDto>>();
        var streetBySegment = new Dictionary<(long?, long?, short?, string?), string>();
        var totalBySegment = new Dictionary<(long?, long?, short?, string?), int>();

        await using (var command = new NpgsqlCommand(
            """
            SELECT
                s."EdgeU", s."EdgeV", s."EdgeKey", s."PlaceId",
                COALESCE(re."Name", p."Name", 'Rue inconnue') AS "Street",
                d.code AS "TypeCode",
                COUNT(*) AS "Count"
            FROM "EdgeSnapshots" s
            CROSS JOIN LATERAL unnest(s."Details") AS d(code)
            LEFT JOIN "RoadEdges" re ON s."EdgeU" = re."U" AND s."EdgeV" = re."V" AND s."EdgeKey" = re."Key"
            LEFT JOIN "Places" p ON s."PlaceId" = p."Id"
            WHERE s."SuitcaseId" = @suitcaseId
              AND date_trunc('day', s."MeasuredAt")::date = @day
              AND s."ItineraryNumber" = @itineraryIndex
            GROUP BY s."EdgeU", s."EdgeV", s."EdgeKey", s."PlaceId", "Street", d.code
            ORDER BY "Street", COUNT(*) DESC
            """,
            connection))
        {
            command.Parameters.AddWithValue("suitcaseId", suitcaseId);
            command.Parameters.AddWithValue("day", dayDate);
            command.Parameters.AddWithValue("itineraryIndex", itineraryIndex);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var edgeU = reader.IsDBNull(0) ? (long?)null : reader.GetInt64(0);
                var edgeV = reader.IsDBNull(1) ? (long?)null : reader.GetInt64(1);
                var edgeKey = reader.IsDBNull(2) ? (short?)null : reader.GetInt16(2);
                var placeId = reader.IsDBNull(3) ? null : reader.GetString(3);
                var street = reader.GetString(4);
                var typeCode = reader.GetInt16(5);
                var count = (int)reader.GetInt64(6);

                var segmentId = (edgeU, edgeV, edgeKey, placeId);
                if (!objectsBySegment.TryGetValue(segmentId, out var objects))
                {
                    objects = [];
                    objectsBySegment[segmentId] = objects;
                    streetBySegment[segmentId] = street;
                    totalBySegment[segmentId] = 0;
                    segmentOrder.Add(segmentId);
                }
                objects.Add(new MeasurementTypeBreakdownDto(typeCode, DetectionTypeCatalog.GetName(typeCode), count));
                totalBySegment[segmentId] += count;
            }
        }

        // Note (Cci moyen) par tronçon, ce jour-là, pour cette suitcase.
        var cciBySegment = new Dictionary<(long?, long?, short?, string?), double>();
        await using (var command = new NpgsqlCommand(
            """
            SELECT "EdgeU", "EdgeV", "EdgeKey", "PlaceId", AVG("Cci") AS "AverageCci"
            FROM "EdgeCciMeasurements"
            WHERE "SuitcaseIds" @> ARRAY[@suitcaseId]
              AND date_trunc('day', "MeasuredAt")::date = @day
              AND "Cci" IS NOT NULL
            GROUP BY "EdgeU", "EdgeV", "EdgeKey", "PlaceId"
            """,
            connection))
        {
            command.Parameters.AddWithValue("suitcaseId", suitcaseId);
            command.Parameters.AddWithValue("day", dayDate);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var edgeU = reader.IsDBNull(0) ? (long?)null : reader.GetInt64(0);
                var edgeV = reader.IsDBNull(1) ? (long?)null : reader.GetInt64(1);
                var edgeKey = reader.IsDBNull(2) ? (short?)null : reader.GetInt16(2);
                var placeId = reader.IsDBNull(3) ? null : reader.GetString(3);
                cciBySegment[(edgeU, edgeV, edgeKey, placeId)] = reader.GetDouble(4);
            }
        }

        var results = segmentOrder
            .Select(segmentId => new ItineraryStreetDetailDto(
                streetBySegment[segmentId],
                totalBySegment[segmentId],
                cciBySegment.TryGetValue(segmentId, out var cci) ? cci : null,
                objectsBySegment[segmentId]))
            .OrderByDescending(r => r.TotalObjects)
            .ToList();

        return Ok(results);
    }
}
