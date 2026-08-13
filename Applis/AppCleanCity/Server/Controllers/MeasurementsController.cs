using System.Text;
using CortexiaAuth.Api.Data;
using CortexiaAuth.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CortexiaAuth.Api.Controllers;

/// <summary>
/// Liste paginée et triable des objets détectés (un objet par ligne). Une ligne EdgeSnapshots
/// peut contenir plusieurs types détectés en un seul passage (colonne Details) : chaque type est
/// exposé comme une ligne distincte ici.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class MeasurementsController(NpgsqlDataSource dataSource, AppDbContext dbContext) : ControllerBase
{
    /// <summary>Réglage "Cacher les objets détectés sans rue associée" (page Paramètres).</summary>
    private async Task<bool> ShouldHideObjectsWithoutStreetAsync(CancellationToken cancellationToken)
    {
        return await dbContext.DetectionDisplaySettings
            .Select(s => s.HideObjectsWithoutStreet)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static readonly HashSet<string> AllowedSortColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        "measuredAt", "type", "street", "latitude", "longitude", "quantity",
    };

    [HttpGet]
    [ProducesResponseType(typeof(PagedMeasurementsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedMeasurementsResponse>> List(
        [FromQuery] int page,
        [FromQuery] int pageSize,
        CancellationToken cancellationToken,
        [FromQuery] string sortBy = "measuredAt",
        [FromQuery] string sortDir = "desc",
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] short? typeCode = null,
        [FromQuery] string? street = null)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize <= 0 ? 50 : pageSize, 1, 200);
        if (!AllowedSortColumns.Contains(sortBy))
        {
            sortBy = "measuredAt";
        }
        var descending = !string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase);
        var hideWithoutStreet = await ShouldHideObjectsWithoutStreetAsync(cancellationToken);

        var orderExpression = sortBy.ToLowerInvariant() switch
        {
            "type" => BuildTypeNameCaseExpression(),
            "street" => "COALESCE(re.\"Name\", p.\"Name\")",
            "latitude" => "ST_Y(s.\"Location\"::geometry)",
            "longitude" => "ST_X(s.\"Location\"::geometry)",
            "quantity" => "COUNT(*)",
            _ => "s.\"MeasuredAt\"",
        };

        var sql = $"""
            SELECT
                s."Id" AS "SnapshotId",
                d.code AS "TypeCode",
                COUNT(*) AS "Quantity",
                s."MeasuredAt",
                COALESCE(re."Name", p."Name") AS "Street",
                ST_Y(s."Location"::geometry) AS "Latitude",
                ST_X(s."Location"::geometry) AS "Longitude",
                COUNT(*) OVER() AS "TotalGroups",
                SUM(COUNT(*)) OVER() AS "TotalObjects"
            FROM "EdgeSnapshots" s
            CROSS JOIN LATERAL unnest(s."Details") AS d(code)
            LEFT JOIN "RoadEdges" re ON s."EdgeU" = re."U" AND s."EdgeV" = re."V" AND s."EdgeKey" = re."Key"
            LEFT JOIN "Places" p ON s."PlaceId" = p."Id"
            WHERE (@startDate::timestamptz IS NULL OR s."MeasuredAt" >= @startDate)
              AND (@endDate::timestamptz IS NULL OR s."MeasuredAt" <= @endDate)
              AND (@typeCode::smallint IS NULL OR d.code = @typeCode)
              AND (@street::text IS NULL OR COALESCE(re."Name", p."Name") = @street)
              AND (@hideWithoutStreet = false OR COALESCE(re."Name", p."Name") IS NOT NULL)
            GROUP BY s."Id", d.code, s."MeasuredAt", re."Name", p."Name", s."Location"
            ORDER BY {orderExpression} {(descending ? "DESC" : "ASC")}, s."Id" ASC, d.code ASC
            LIMIT @pageSize OFFSET @offset
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("pageSize", pageSize);
        command.Parameters.AddWithValue("offset", (page - 1) * pageSize);
        command.Parameters.AddWithValue("startDate", (object?)startDate ?? DBNull.Value);
        command.Parameters.AddWithValue("endDate", (object?)endDate ?? DBNull.Value);
        command.Parameters.AddWithValue("typeCode", (object?)typeCode ?? DBNull.Value);
        command.Parameters.AddWithValue("street", (object?)street ?? DBNull.Value);
        command.Parameters.AddWithValue("hideWithoutStreet", hideWithoutStreet);

        var items = new List<MeasurementDto>();
        var totalGroups = 0;
        var totalObjects = 0;
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                var rowTypeCode = reader.GetInt16(reader.GetOrdinal("TypeCode"));
                items.Add(new MeasurementDto(
                    reader.GetInt64(reader.GetOrdinal("SnapshotId")),
                    rowTypeCode,
                    DetectionTypeCatalog.GetName(rowTypeCode),
                    (int)reader.GetInt64(reader.GetOrdinal("Quantity")),
                    reader.GetDateTime(reader.GetOrdinal("MeasuredAt")),
                    reader.IsDBNull(reader.GetOrdinal("Street")) ? null : reader.GetString(reader.GetOrdinal("Street")),
                    reader.GetDouble(reader.GetOrdinal("Latitude")),
                    reader.GetDouble(reader.GetOrdinal("Longitude"))));
                totalGroups = (int)reader.GetInt64(reader.GetOrdinal("TotalGroups"));
                totalObjects = (int)reader.GetInt64(reader.GetOrdinal("TotalObjects"));
            }
        }

        return Ok(new PagedMeasurementsResponse(totalGroups, totalObjects, page, pageSize, items));
    }

    /// <summary>Répartition du nombre d'objets détectés par type, sur la période donnée (pour le camembert).</summary>
    [HttpGet("type-breakdown")]
    [ProducesResponseType(typeof(IEnumerable<MeasurementTypeBreakdownDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<MeasurementTypeBreakdownDto>>> GetTypeBreakdown(
        CancellationToken cancellationToken,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? street = null)
    {
        var hideWithoutStreet = await ShouldHideObjectsWithoutStreetAsync(cancellationToken);

        const string sql = """
            SELECT d.code AS "TypeCode", COUNT(*) AS "Count"
            FROM "EdgeSnapshots" s
            CROSS JOIN LATERAL unnest(s."Details") AS d(code)
            LEFT JOIN "RoadEdges" re ON s."EdgeU" = re."U" AND s."EdgeV" = re."V" AND s."EdgeKey" = re."Key"
            LEFT JOIN "Places" p ON s."PlaceId" = p."Id"
            WHERE (@startDate::timestamptz IS NULL OR s."MeasuredAt" >= @startDate)
              AND (@endDate::timestamptz IS NULL OR s."MeasuredAt" <= @endDate)
              AND (@street::text IS NULL OR COALESCE(re."Name", p."Name") = @street)
              AND (@hideWithoutStreet = false OR COALESCE(re."Name", p."Name") IS NOT NULL)
            GROUP BY d.code
            ORDER BY COUNT(*) DESC
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("startDate", (object?)startDate ?? DBNull.Value);
        command.Parameters.AddWithValue("endDate", (object?)endDate ?? DBNull.Value);
        command.Parameters.AddWithValue("street", (object?)street ?? DBNull.Value);
        command.Parameters.AddWithValue("hideWithoutStreet", hideWithoutStreet);

        var items = new List<MeasurementTypeBreakdownDto>();
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                var typeCode = reader.GetInt16(reader.GetOrdinal("TypeCode"));
                items.Add(new MeasurementTypeBreakdownDto(typeCode, DetectionTypeCatalog.GetName(typeCode), (int)reader.GetInt64(reader.GetOrdinal("Count"))));
            }
        }

        return Ok(items);
    }

    /// <summary>
    /// Points géolocalisés (un par snapshot/type détecté) sur la période, non paginés, pour la carte de
    /// concentration de l'onglet Détails : le regroupement visuel par zoom se fait côté client.
    /// </summary>
    [HttpGet("points")]
    [ProducesResponseType(typeof(IEnumerable<MeasurementPointDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<MeasurementPointDto>>> GetPoints(
        CancellationToken cancellationToken,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] short? typeCode = null,
        [FromQuery] string? street = null)
    {
        var hideWithoutStreet = await ShouldHideObjectsWithoutStreetAsync(cancellationToken);

        const string sql = """
            SELECT
                ST_Y(s."Location"::geometry) AS "Latitude",
                ST_X(s."Location"::geometry) AS "Longitude",
                d.code AS "TypeCode",
                COUNT(*) AS "Quantity",
                COALESCE(re."Name", p."Name") AS "Street"
            FROM "EdgeSnapshots" s
            CROSS JOIN LATERAL unnest(s."Details") AS d(code)
            LEFT JOIN "RoadEdges" re ON s."EdgeU" = re."U" AND s."EdgeV" = re."V" AND s."EdgeKey" = re."Key"
            LEFT JOIN "Places" p ON s."PlaceId" = p."Id"
            WHERE (@startDate::timestamptz IS NULL OR s."MeasuredAt" >= @startDate)
              AND (@endDate::timestamptz IS NULL OR s."MeasuredAt" <= @endDate)
              AND (@typeCode::smallint IS NULL OR d.code = @typeCode)
              AND (@street::text IS NULL OR COALESCE(re."Name", p."Name") = @street)
              AND (@hideWithoutStreet = false OR COALESCE(re."Name", p."Name") IS NOT NULL)
            GROUP BY s."Id", d.code, s."Location", re."Name", p."Name"
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("startDate", (object?)startDate ?? DBNull.Value);
        command.Parameters.AddWithValue("endDate", (object?)endDate ?? DBNull.Value);
        command.Parameters.AddWithValue("typeCode", (object?)typeCode ?? DBNull.Value);
        command.Parameters.AddWithValue("street", (object?)street ?? DBNull.Value);
        command.Parameters.AddWithValue("hideWithoutStreet", hideWithoutStreet);

        var items = new List<MeasurementPointDto>();
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                var rowTypeCode = reader.GetInt16(reader.GetOrdinal("TypeCode"));
                var streetOrdinal = reader.GetOrdinal("Street");
                items.Add(new MeasurementPointDto(
                    reader.GetDouble(reader.GetOrdinal("Latitude")),
                    reader.GetDouble(reader.GetOrdinal("Longitude")),
                    rowTypeCode,
                    DetectionTypeCatalog.GetName(rowTypeCode),
                    (int)reader.GetInt64(reader.GetOrdinal("Quantity")),
                    reader.IsDBNull(streetOrdinal) ? null : reader.GetString(streetOrdinal)));
            }
        }

        return Ok(items);
    }

    /// <summary>Rues distinctes ayant au moins une détection sur la période (et le type, si filtré).</summary>
    [HttpGet("streets")]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<string>>> GetStreets(
        CancellationToken cancellationToken,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] short? typeCode = null)
    {
        const string sql = """
            SELECT DISTINCT COALESCE(re."Name", p."Name") AS "Street"
            FROM "EdgeSnapshots" s
            CROSS JOIN LATERAL unnest(s."Details") AS d(code)
            LEFT JOIN "RoadEdges" re ON s."EdgeU" = re."U" AND s."EdgeV" = re."V" AND s."EdgeKey" = re."Key"
            LEFT JOIN "Places" p ON s."PlaceId" = p."Id"
            WHERE (@startDate::timestamptz IS NULL OR s."MeasuredAt" >= @startDate)
              AND (@endDate::timestamptz IS NULL OR s."MeasuredAt" <= @endDate)
              AND (@typeCode::smallint IS NULL OR d.code = @typeCode)
              AND COALESCE(re."Name", p."Name") IS NOT NULL
            ORDER BY "Street"
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("startDate", (object?)startDate ?? DBNull.Value);
        command.Parameters.AddWithValue("endDate", (object?)endDate ?? DBNull.Value);
        command.Parameters.AddWithValue("typeCode", (object?)typeCode ?? DBNull.Value);

        var streets = new List<string>();
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                streets.Add(reader.GetString(0));
            }
        }

        return Ok(streets);
    }

    /// <summary>
    /// Construit un CASE SQL à partir de DetectionTypeCatalog pour permettre un tri alphabétique
    /// sur le nom du type, sans dupliquer la table de correspondance côté base.
    /// </summary>
    private static string BuildTypeNameCaseExpression()
    {
        var builder = new StringBuilder("CASE d.code");
        foreach (var (code, name) in DetectionTypeCatalog.Names)
        {
            builder.Append(" WHEN ").Append(code).Append(" THEN '").Append(name.Replace("'", "''")).Append('\'');
        }
        builder.Append(" ELSE 'Type ' || d.code::text END");
        return builder.ToString();
    }
}
