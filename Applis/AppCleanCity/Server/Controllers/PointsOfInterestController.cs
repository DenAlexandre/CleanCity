using System.Text;
using CortexiaAuth.Api.Data;
using CortexiaAuth.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using Npgsql;

namespace CortexiaAuth.Api.Controllers;

/// <summary>
/// Points d'intérêt saisis par un administrateur du site (à ne pas confondre avec les "Places"
/// importées automatiquement depuis Cortexia). Lecture libre, écriture réservée aux comptes
/// ayant le droit "Gestion des comptes" (même mécanisme d'authentification que AuthController).
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PointsOfInterestController(AppDbContext dbContext, NpgsqlDataSource dataSource, PasswordHasher<AppUser> passwordHasher) : ControllerBase
{
    private const int Srid = 4326;

    private const double DefaultRadiusMeters = 500;

    private static readonly HashSet<string> AllowedMeasurementSortColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        "measuredAt", "type", "street", "latitude", "longitude", "quantity",
    };

    /// <summary>Rayon configurable (page Paramètres) ; même valeur que DashboardController.GetPointOfInterestScores.</summary>
    private async Task<double> GetRadiusMetersAsync(CancellationToken cancellationToken)
    {
        var radius = await dbContext.PointOfInterestSettings.Select(s => (double?)s.RadiusMeters).FirstOrDefaultAsync(cancellationToken);
        return radius ?? DefaultRadiusMeters;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<PointOfInterestDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<PointOfInterestDto>>> List(CancellationToken cancellationToken)
    {
        var points = await dbContext.PointsOfInterest.AsNoTracking().OrderBy(p => p.Name).ToListAsync(cancellationToken);
        return Ok(points.Select(ToDto));
    }

    /// <summary>Note (Cci moyen sur 500m) de chaque point d'intérêt sur la période donnée.</summary>
    [HttpGet("scores")]
    [ProducesResponseType(typeof(IEnumerable<PointOfInterestScoreDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<PointOfInterestScoreDto>>> GetScores(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT
                poi."Id", poi."Name", poi."Description", poi."Category",
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
                ) AS "AverageCci"
            FROM "PointsOfInterest" poi
            ORDER BY poi."Category", poi."Name"
            """;

        var radiusMeters = await GetRadiusMetersAsync(cancellationToken);

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("startDate", startDate);
        command.Parameters.AddWithValue("endDate", endDate);
        command.Parameters.AddWithValue("radius", radiusMeters);

        var items = new List<PointOfInterestScoreDto>();
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                items.Add(new PointOfInterestScoreDto(
                    reader.GetGuid(0),
                    reader.GetString(1),
                    reader.IsDBNull(2) ? null : reader.GetString(2),
                    reader.GetString(3),
                    reader.IsDBNull(4) ? null : reader.GetDouble(4)));
            }
        }

        return Ok(items);
    }

    /// <summary>Répartition par type des objets détectés à proximité (500m) de ce point d'intérêt, sur la période.</summary>
    [HttpGet("{id:guid}/objects")]
    [ProducesResponseType(typeof(IEnumerable<PointOfInterestObjectBreakdownDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<PointOfInterestObjectBreakdownDto>>> GetObjectBreakdown(
        Guid id,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        CancellationToken cancellationToken)
    {
        if (!await dbContext.PointsOfInterest.AsNoTracking().AnyAsync(p => p.Id == id, cancellationToken))
        {
            return NotFound();
        }

        const string sql = """
            SELECT d.code AS "TypeCode", COUNT(*) AS "Count"
            FROM "EdgeSnapshots" s
            CROSS JOIN LATERAL unnest(s."Details") AS d(code)
            JOIN "PointsOfInterest" poi ON poi."Id" = @poiId
            WHERE ST_DWithin(s."Location", poi."Location", @radius)
              AND s."MeasuredAt" >= @startDate AND s."MeasuredAt" <= @endDate
            GROUP BY d.code
            ORDER BY COUNT(*) DESC
            """;

        var radiusMeters = await GetRadiusMetersAsync(cancellationToken);

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("poiId", id);
        command.Parameters.AddWithValue("startDate", startDate);
        command.Parameters.AddWithValue("endDate", endDate);
        command.Parameters.AddWithValue("radius", radiusMeters);

        var items = new List<PointOfInterestObjectBreakdownDto>();
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                var typeCode = reader.GetInt16(0);
                items.Add(new PointOfInterestObjectBreakdownDto(typeCode, DetectionTypeCatalog.GetName(typeCode), (int)reader.GetInt64(1)));
            }
        }

        return Ok(items);
    }

    /// <summary>
    /// Liste paginée et triable des objets détectés à proximité (rayon configuré) d'au moins un point
    /// d'intérêt, filtrable par catégorie et/ou par point d'intérêt précis. Même forme de réponse que
    /// MeasurementsController.List, réutilisée telle quelle côté frontend.
    /// </summary>
    [HttpGet("measurements")]
    [ProducesResponseType(typeof(PagedMeasurementsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedMeasurementsResponse>> GetMeasurementsNearPoints(
        [FromQuery] int page,
        [FromQuery] int pageSize,
        CancellationToken cancellationToken,
        [FromQuery] string sortBy = "measuredAt",
        [FromQuery] string sortDir = "desc",
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] short? typeCode = null,
        [FromQuery] string? category = null,
        [FromQuery] Guid? poiId = null)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize <= 0 ? 50 : pageSize, 1, 200);
        if (!AllowedMeasurementSortColumns.Contains(sortBy))
        {
            sortBy = "measuredAt";
        }
        var descending = !string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase);

        var orderExpression = sortBy.ToLowerInvariant() switch
        {
            "type" => BuildTypeNameCaseExpression(),
            "street" => "COALESCE(re.\"Name\", p.\"Name\")",
            "latitude" => "ST_Y(s.\"Location\"::geometry)",
            "longitude" => "ST_X(s.\"Location\"::geometry)",
            "quantity" => "COUNT(*)",
            _ => "s.\"MeasuredAt\"",
        };

        var radiusMeters = await GetRadiusMetersAsync(cancellationToken);

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
              AND EXISTS (
                  SELECT 1 FROM "PointsOfInterest" poi
                  WHERE ST_DWithin(s."Location", poi."Location", @radius)
                    AND (@poiId::uuid IS NULL OR poi."Id" = @poiId)
                    AND (@category::text IS NULL OR poi."Category" = @category)
              )
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
        command.Parameters.AddWithValue("category", (object?)category ?? DBNull.Value);
        command.Parameters.AddWithValue("poiId", (object?)poiId ?? DBNull.Value);
        command.Parameters.AddWithValue("radius", radiusMeters);

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

    /// <summary>
    /// Répartition par type des objets détectés à proximité d'au moins un point d'intérêt (mêmes
    /// filtres catégorie/point d'intérêt que GetMeasurementsNearPoints), pour le camembert de
    /// l'onglet "Points d'intérêts" de la page Liste des mesures.
    /// </summary>
    [HttpGet("measurements/type-breakdown")]
    [ProducesResponseType(typeof(IEnumerable<MeasurementTypeBreakdownDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<MeasurementTypeBreakdownDto>>> GetMeasurementsNearPointsTypeBreakdown(
        CancellationToken cancellationToken,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? category = null,
        [FromQuery] Guid? poiId = null)
    {
        var radiusMeters = await GetRadiusMetersAsync(cancellationToken);

        const string sql = """
            SELECT d.code AS "TypeCode", COUNT(*) AS "Count"
            FROM "EdgeSnapshots" s
            CROSS JOIN LATERAL unnest(s."Details") AS d(code)
            WHERE (@startDate::timestamptz IS NULL OR s."MeasuredAt" >= @startDate)
              AND (@endDate::timestamptz IS NULL OR s."MeasuredAt" <= @endDate)
              AND EXISTS (
                  SELECT 1 FROM "PointsOfInterest" poi
                  WHERE ST_DWithin(s."Location", poi."Location", @radius)
                    AND (@poiId::uuid IS NULL OR poi."Id" = @poiId)
                    AND (@category::text IS NULL OR poi."Category" = @category)
              )
            GROUP BY d.code
            ORDER BY COUNT(*) DESC
            """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("startDate", (object?)startDate ?? DBNull.Value);
        command.Parameters.AddWithValue("endDate", (object?)endDate ?? DBNull.Value);
        command.Parameters.AddWithValue("category", (object?)category ?? DBNull.Value);
        command.Parameters.AddWithValue("poiId", (object?)poiId ?? DBNull.Value);
        command.Parameters.AddWithValue("radius", radiusMeters);

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
    /// Construit un CASE SQL à partir de DetectionTypeCatalog pour permettre un tri alphabétique sur le
    /// nom du type (même logique que MeasurementsController.BuildTypeNameCaseExpression).
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

    [HttpPost]
    [ProducesResponseType(typeof(PointOfInterestDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PointOfInterestDto>> Create(
        [FromBody] SavePointOfInterestRequest request,
        [FromHeader(Name = "X-Admin-Username")] string? adminUsername,
        [FromHeader(Name = "X-Admin-Password")] string? adminPassword,
        CancellationToken cancellationToken)
    {
        var authError = await AuthenticateAdminAsync(adminUsername, adminPassword, cancellationToken);
        if (authError is not null)
        {
            return authError;
        }

        var point = new PointOfInterest
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            Category = request.Category,
            Location = new Point(request.Longitude, request.Latitude) { SRID = Srid },
            CreatedAtUtc = DateTime.UtcNow,
        };

        dbContext.PointsOfInterest.Add(point);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(List), ToDto(point));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] SavePointOfInterestRequest request,
        [FromHeader(Name = "X-Admin-Username")] string? adminUsername,
        [FromHeader(Name = "X-Admin-Password")] string? adminPassword,
        CancellationToken cancellationToken)
    {
        var authError = await AuthenticateAdminAsync(adminUsername, adminPassword, cancellationToken);
        if (authError is not null)
        {
            return authError;
        }

        var point = await dbContext.PointsOfInterest.SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (point is null)
        {
            return NotFound();
        }

        point.Name = request.Name;
        point.Description = request.Description;
        point.Category = request.Category;
        point.Location = new Point(request.Longitude, request.Latitude) { SRID = Srid };
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromHeader(Name = "X-Admin-Username")] string? adminUsername,
        [FromHeader(Name = "X-Admin-Password")] string? adminPassword,
        CancellationToken cancellationToken)
    {
        var authError = await AuthenticateAdminAsync(adminUsername, adminPassword, cancellationToken);
        if (authError is not null)
        {
            return authError;
        }

        var point = await dbContext.PointsOfInterest.SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (point is null)
        {
            return NotFound();
        }

        dbContext.PointsOfInterest.Remove(point);
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private static PointOfInterestDto ToDto(PointOfInterest point) =>
        new(point.Id, point.Name, point.Description, point.Category, point.Location.Y, point.Location.X, point.CreatedAtUtc);

    /// <summary>
    /// Authentifie l'appelant comme administrateur via les headers X-Admin-Username / X-Admin-Password
    /// (même contrat que AuthController.AuthenticateAdminAsync : pas de session/JWT côté site).
    /// </summary>
    private async Task<ActionResult?> AuthenticateAdminAsync(string? adminUsername, string? adminPassword, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(adminUsername) || string.IsNullOrEmpty(adminPassword))
        {
            return Unauthorized(new
            {
                error = "Authentification administrateur requise. Renseignez les champs 'X-Admin-Username' et 'X-Admin-Password' de cette requête " +
                        "(identifiant/mot de passe SITE, pas Cortexia, d'un compte ayant le droit 'Gestion des comptes').",
            });
        }

        var admin = await dbContext.AppUsers.Include(u => u.Role).SingleOrDefaultAsync(u => u.Username == adminUsername, cancellationToken);
        if (admin is null || passwordHasher.VerifyHashedPassword(admin, admin.PasswordHash, adminPassword) == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new { error = $"Aucun compte administrateur ne correspond à l'identifiant '{adminUsername}' avec ce mot de passe." });
        }

        if (!admin.Role.Permissions.ManageAccounts)
        {
            return Unauthorized(new { error = $"Le compte '{adminUsername}' existe mais n'a pas le droit 'Gestion des comptes'." });
        }

        return null;
    }
}
