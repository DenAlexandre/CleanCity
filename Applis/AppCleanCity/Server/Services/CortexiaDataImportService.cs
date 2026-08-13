using System.Text.Json;
using NetTopologySuite.Geometries;
using Npgsql;
using NpgsqlTypes;

namespace CortexiaAuth.Api.Services;

public class CortexiaDataImportService(NpgsqlDataSource dataSource) : IDataImportService
{
    private const int Srid = 4326;

    private record RoadEdgeRow(long U, long V, short Key, long[] OsmIds, string? Highway, string? Name, double LengthMeters, Geometry Geometry, string PropertiesJson);

    private record PlaceRow(string Id, string? Name, int CityId, Geometry Geometry);

    private record SnapshotRow(
        long? EdgeU, long? EdgeV, short? EdgeKey, string? PlaceId, float Direction, float SpeedMs, string SuitcaseId,
        Point Location, short[] Details, DateTime MeasuredAt, DateTime PostedAt, int CityId);

    private record CciRow(
        long? EdgeU, long? EdgeV, short? EdgeKey, string? PlaceId, DateTime MeasuredAt, DateTime PostedAt, float Direction,
        float? Cci, float? CciCustom, bool HasMeasure, string[] SuitcaseIds,
        float? RateLeaves, float? RateCigarettes, float? RateGums, float? RatePapers, float? RateGrits,
        float? RateBottles, float? RateExcrements, float? RateGlassDebris, float? RateSyringues,
        string CustomCciPerTypesJson);

    /// <summary>id Cortexia : soit un tuple d'edge "(u, v, key)", soit un id de place (Elasticsearch).</summary>
    private static (long? U, long? V, short? Key, string? PlaceId) ParseMeasurementId(string id) =>
        CortexiaIdParser.TryParseEdgeId(id, out var edge)
            ? (edge.U, edge.V, edge.Key, null)
            : (null, null, null, id);

    public async Task<ImportResult> ImportRoadEdgesAsync(Stream geoJsonStream, CancellationToken cancellationToken)
    {
        using var document = await ParseJsonAsync(geoJsonStream, cancellationToken);

        if (document.RootElement.ValueKind != JsonValueKind.Object
            || !document.RootElement.TryGetProperty("type", out var typeElement) || typeElement.GetString() != "FeatureCollection"
            || !document.RootElement.TryGetProperty("features", out var featuresElement) || featuresElement.ValueKind != JsonValueKind.Array)
        {
            throw new ImportValidationException("Format GeoJSON invalide : un objet FeatureCollection avec un tableau \"features\" est attendu.");
        }

        var features = featuresElement.EnumerateArray().ToList();

        // edges_and_places mélange deux types de features : les edges du graphe routier
        // (LineString, id OSMnx "(u, v, key)") et les places / POI (Point ou Polygon, id Elasticsearch).
        var edgeRows = ParseAll(features.Where(IsLineStringFeature), ParseRoadEdgeFeature, "edge");
        var placeRows = ParseAll(features.Where(f => !IsLineStringFeature(f)), ParsePlaceFeature, "place");

        await using var connection = await OpenConnectionAsync(cancellationToken);

        await ImportRoadEdgeRowsAsync(connection, edgeRows, cancellationToken);
        await ImportPlaceRowsAsync(connection, placeRows, cancellationToken);

        return new ImportResult(edgeRows.Count + placeRows.Count);
    }

    private static bool IsLineStringFeature(JsonElement feature) =>
        feature.TryGetProperty("geometry", out var geometry)
        && geometry.TryGetProperty("type", out var type)
        && type.GetString() == "LineString";

    private static async Task ImportRoadEdgeRowsAsync(NpgsqlConnection connection, List<RoadEdgeRow> rows, CancellationToken cancellationToken)
    {
        await ExecuteAsync(
            connection,
            """
            CREATE TEMP TABLE "RoadEdgesStaging" (
                "U" bigint NOT NULL, "V" bigint NOT NULL, "Key" smallint NOT NULL,
                "OsmIds" bigint[] NOT NULL, "Highway" text, "Name" text,
                "LengthMeters" double precision NOT NULL, "Geometry" geometry NOT NULL, "PropertiesJson" jsonb NOT NULL
            );
            """,
            cancellationToken);

        await using (var writer = await connection.BeginBinaryImportAsync(
            """COPY "RoadEdgesStaging" ("U","V","Key","OsmIds","Highway","Name","LengthMeters","Geometry","PropertiesJson") FROM STDIN (FORMAT BINARY)""",
            cancellationToken))
        {
            foreach (var row in rows)
            {
                await writer.StartRowAsync(cancellationToken);
                await writer.WriteAsync(row.U, NpgsqlDbType.Bigint, cancellationToken);
                await writer.WriteAsync(row.V, NpgsqlDbType.Bigint, cancellationToken);
                await writer.WriteAsync(row.Key, NpgsqlDbType.Smallint, cancellationToken);
                await writer.WriteAsync(row.OsmIds, NpgsqlDbType.Array | NpgsqlDbType.Bigint, cancellationToken);
                await writer.WriteAsync(row.Highway, NpgsqlDbType.Text, cancellationToken);
                await writer.WriteAsync(row.Name, NpgsqlDbType.Text, cancellationToken);
                await writer.WriteAsync(row.LengthMeters, NpgsqlDbType.Double, cancellationToken);
                await writer.WriteAsync(row.Geometry, "geometry", cancellationToken);
                await writer.WriteAsync(row.PropertiesJson, NpgsqlDbType.Jsonb, cancellationToken);
            }

            await writer.CompleteAsync(cancellationToken);
        }

        await ExecuteAsync(
            connection,
            """
            INSERT INTO "RoadEdges" ("U","V","Key","OsmIds","Highway","Name","LengthMeters","Geometry","PropertiesJson")
            SELECT "U","V","Key","OsmIds","Highway","Name","LengthMeters","Geometry","PropertiesJson" FROM "RoadEdgesStaging"
            ON CONFLICT ("U","V","Key") DO UPDATE SET
                "OsmIds" = EXCLUDED."OsmIds", "Highway" = EXCLUDED."Highway", "Name" = EXCLUDED."Name",
                "LengthMeters" = EXCLUDED."LengthMeters", "Geometry" = EXCLUDED."Geometry", "PropertiesJson" = EXCLUDED."PropertiesJson";
            """,
            cancellationToken);
    }

    private static async Task ImportPlaceRowsAsync(NpgsqlConnection connection, List<PlaceRow> rows, CancellationToken cancellationToken)
    {
        if (rows.Count == 0)
        {
            return;
        }

        await ExecuteAsync(
            connection,
            """
            CREATE TEMP TABLE "PlacesStaging" (
                "Id" text NOT NULL, "Name" text, "CityId" integer NOT NULL, "Geometry" geometry NOT NULL
            );
            """,
            cancellationToken);

        await using (var writer = await connection.BeginBinaryImportAsync(
            """COPY "PlacesStaging" ("Id","Name","CityId","Geometry") FROM STDIN (FORMAT BINARY)""",
            cancellationToken))
        {
            foreach (var row in rows)
            {
                await writer.StartRowAsync(cancellationToken);
                await writer.WriteAsync(row.Id, NpgsqlDbType.Text, cancellationToken);
                await writer.WriteAsync(row.Name, NpgsqlDbType.Text, cancellationToken);
                await writer.WriteAsync(row.CityId, NpgsqlDbType.Integer, cancellationToken);
                await writer.WriteAsync(row.Geometry, "geometry", cancellationToken);
            }

            await writer.CompleteAsync(cancellationToken);
        }

        await ExecuteAsync(
            connection,
            """
            INSERT INTO "Places" ("Id","Name","CityId","Geometry")
            SELECT "Id","Name","CityId","Geometry" FROM "PlacesStaging"
            ON CONFLICT ("Id") DO UPDATE SET
                "Name" = EXCLUDED."Name", "CityId" = EXCLUDED."CityId", "Geometry" = EXCLUDED."Geometry";
            """,
            cancellationToken);
    }

    public async Task<ImportResult> ImportSnapshotsAsync(Stream jsonStream, CancellationToken cancellationToken)
    {
        using var document = await ParseJsonAsync(jsonStream, cancellationToken);
        EnsureArray(document, "aggregated_snapshots doit être un tableau JSON.");

        var rows = ParseAll(document.RootElement.EnumerateArray(), ParseSnapshot, "snapshot");
        if (rows.Count == 0)
        {
            return new ImportResult(0);
        }

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await EnsureMonthlyPartitionsAsync(connection, "EdgeSnapshots", rows.Select(r => r.MeasuredAt), cancellationToken);

        await using var writer = await connection.BeginBinaryImportAsync(
            """COPY "EdgeSnapshots" ("EdgeU","EdgeV","EdgeKey","PlaceId","Direction","SpeedMs","SuitcaseId","Location","Details","MeasuredAt","PostedAt","CityId") FROM STDIN (FORMAT BINARY)""",
            cancellationToken);

        foreach (var row in rows)
        {
            await writer.StartRowAsync(cancellationToken);
            await writer.WriteAsync(row.EdgeU, NpgsqlDbType.Bigint, cancellationToken);
            await writer.WriteAsync(row.EdgeV, NpgsqlDbType.Bigint, cancellationToken);
            await writer.WriteAsync(row.EdgeKey, NpgsqlDbType.Smallint, cancellationToken);
            await writer.WriteAsync(row.PlaceId, NpgsqlDbType.Text, cancellationToken);
            await writer.WriteAsync(row.Direction, NpgsqlDbType.Real, cancellationToken);
            await writer.WriteAsync(row.SpeedMs, NpgsqlDbType.Real, cancellationToken);
            await writer.WriteAsync(row.SuitcaseId, NpgsqlDbType.Text, cancellationToken);
            await writer.WriteAsync(row.Location, "geography", cancellationToken);
            await writer.WriteAsync(row.Details, NpgsqlDbType.Array | NpgsqlDbType.Smallint, cancellationToken);
            await writer.WriteAsync(row.MeasuredAt, NpgsqlDbType.TimestampTz, cancellationToken);
            await writer.WriteAsync(row.PostedAt, NpgsqlDbType.TimestampTz, cancellationToken);
            await writer.WriteAsync(row.CityId, NpgsqlDbType.Integer, cancellationToken);
        }

        await writer.CompleteAsync(cancellationToken);
        return new ImportResult(rows.Count);
    }

    public async Task<ImportResult> ImportCciMeasurementsAsync(Stream jsonStream, CancellationToken cancellationToken)
    {
        using var document = await ParseJsonAsync(jsonStream, cancellationToken);
        EnsureArray(document, "edges_and_places_cci doit être un tableau JSON.");

        var rows = ParseAll(document.RootElement.EnumerateArray(), ParseCci, "cci");
        if (rows.Count == 0)
        {
            return new ImportResult(0);
        }

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await EnsureMonthlyPartitionsAsync(connection, "EdgeCciMeasurements", rows.Select(r => r.MeasuredAt), cancellationToken);

        await using var writer = await connection.BeginBinaryImportAsync(
            """
            COPY "EdgeCciMeasurements" (
                "EdgeU","EdgeV","EdgeKey","PlaceId","MeasuredAt","PostedAt","Direction","Cci","CciCustom","HasMeasure","SuitcaseIds",
                "RateLeaves","RateCigarettes","RateGums","RatePapers","RateGrits","RateBottles","RateExcrements","RateGlassDebris","RateSyringues",
                "CustomCciPerTypesJson"
            ) FROM STDIN (FORMAT BINARY)
            """,
            cancellationToken);

        foreach (var row in rows)
        {
            await writer.StartRowAsync(cancellationToken);
            await writer.WriteAsync(row.EdgeU, NpgsqlDbType.Bigint, cancellationToken);
            await writer.WriteAsync(row.EdgeV, NpgsqlDbType.Bigint, cancellationToken);
            await writer.WriteAsync(row.EdgeKey, NpgsqlDbType.Smallint, cancellationToken);
            await writer.WriteAsync(row.PlaceId, NpgsqlDbType.Text, cancellationToken);
            await writer.WriteAsync(row.MeasuredAt, NpgsqlDbType.TimestampTz, cancellationToken);
            await writer.WriteAsync(row.PostedAt, NpgsqlDbType.TimestampTz, cancellationToken);
            await writer.WriteAsync(row.Direction, NpgsqlDbType.Real, cancellationToken);
            await writer.WriteAsync(row.Cci, NpgsqlDbType.Real, cancellationToken);
            await writer.WriteAsync(row.CciCustom, NpgsqlDbType.Real, cancellationToken);
            await writer.WriteAsync(row.HasMeasure, NpgsqlDbType.Boolean, cancellationToken);
            await writer.WriteAsync(row.SuitcaseIds, NpgsqlDbType.Array | NpgsqlDbType.Text, cancellationToken);
            await writer.WriteAsync(row.RateLeaves, NpgsqlDbType.Real, cancellationToken);
            await writer.WriteAsync(row.RateCigarettes, NpgsqlDbType.Real, cancellationToken);
            await writer.WriteAsync(row.RateGums, NpgsqlDbType.Real, cancellationToken);
            await writer.WriteAsync(row.RatePapers, NpgsqlDbType.Real, cancellationToken);
            await writer.WriteAsync(row.RateGrits, NpgsqlDbType.Real, cancellationToken);
            await writer.WriteAsync(row.RateBottles, NpgsqlDbType.Real, cancellationToken);
            await writer.WriteAsync(row.RateExcrements, NpgsqlDbType.Real, cancellationToken);
            await writer.WriteAsync(row.RateGlassDebris, NpgsqlDbType.Real, cancellationToken);
            await writer.WriteAsync(row.RateSyringues, NpgsqlDbType.Real, cancellationToken);
            await writer.WriteAsync(row.CustomCciPerTypesJson, NpgsqlDbType.Jsonb, cancellationToken);
        }

        await writer.CompleteAsync(cancellationToken);
        return new ImportResult(rows.Count);
    }

    public async Task<int> CleanupDuplicateMeasurementsAsync(CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);

        var deletedSnapshots = await ExecuteDeleteAsync(
            connection,
            """
            DELETE FROM "EdgeSnapshots" WHERE "Id" IN (
                SELECT "Id" FROM (
                    SELECT "Id", ROW_NUMBER() OVER (
                        PARTITION BY "EdgeU", "EdgeV", "EdgeKey", "PlaceId", "MeasuredAt", "PostedAt", "SuitcaseId", "Location", "Details"
                        ORDER BY "Id"
                    ) AS rn
                    FROM "EdgeSnapshots"
                ) dedup
                WHERE dedup.rn > 1
            )
            """,
            cancellationToken);

        var deletedCci = await ExecuteDeleteAsync(
            connection,
            """
            DELETE FROM "EdgeCciMeasurements" WHERE "Id" IN (
                SELECT "Id" FROM (
                    SELECT "Id", ROW_NUMBER() OVER (
                        PARTITION BY "EdgeU", "EdgeV", "EdgeKey", "PlaceId", "MeasuredAt", "PostedAt"
                        ORDER BY "Id"
                    ) AS rn
                    FROM "EdgeCciMeasurements"
                ) dedup
                WHERE dedup.rn > 1
            )
            """,
            cancellationToken);

        return deletedSnapshots + deletedCci;
    }

    private static async Task<int> ExecuteDeleteAsync(NpgsqlConnection connection, string sql, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<int> AssignItineraryNumbersAsync(CancellationToken cancellationToken)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            UPDATE "EdgeSnapshots" s
            SET "ItineraryNumber" = computed."ItineraryNumber"
            FROM (
                SELECT
                    "Id",
                    1 + FLOOR(
                        EXTRACT(EPOCH FROM ("MeasuredAt" - MIN("MeasuredAt") OVER (
                            PARTITION BY "SuitcaseId", date_trunc('day', "MeasuredAt")
                        ))) / 25200
                    )::int AS "ItineraryNumber"
                FROM "EdgeSnapshots"
            ) computed
            WHERE s."Id" = computed."Id"
              AND s."ItineraryNumber" IS DISTINCT FROM computed."ItineraryNumber"
            """,
            connection);
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static RoadEdgeRow ParseRoadEdgeFeature(JsonElement feature)
    {
        var properties = feature.GetProperty("properties");
        var (u, v, key) = CortexiaIdParser.ParseEdgeId(properties.GetProperty("id").GetString()!);

        return new RoadEdgeRow(
            u, v, key,
            properties.GetLongOrArray("osmid"),
            properties.GetFlatString("highway"),
            properties.GetFlatString("name"),
            properties.GetProperty("length").GetDouble(),
            ParseGeometry(feature.GetProperty("geometry")),
            properties.GetRawText());
    }

    private static PlaceRow ParsePlaceFeature(JsonElement feature)
    {
        var properties = feature.GetProperty("properties");

        return new PlaceRow(
            properties.GetProperty("id").GetString()!,
            properties.GetFlatString("name"),
            properties.GetProperty("city_id").GetInt32(),
            ParseGeometry(feature.GetProperty("geometry")));
    }

    private static Geometry ParseGeometry(JsonElement geometry)
    {
        var coordinates = geometry.GetProperty("coordinates");

        return geometry.GetProperty("type").GetString() switch
        {
            "Point" => new Point(coordinates[0].GetDouble(), coordinates[1].GetDouble()) { SRID = Srid },
            "Polygon" => ParsePolygon(coordinates),
            _ => new LineString(ParseCoordinateRing(coordinates)) { SRID = Srid },
        };
    }

    private static Polygon ParsePolygon(JsonElement rings)
    {
        var shell = new LinearRing(ParseCoordinateRing(rings[0])) { SRID = Srid };
        var holes = rings.GetArrayLength() > 1
            ? rings.EnumerateArray().Skip(1).Select(r => new LinearRing(ParseCoordinateRing(r)) { SRID = Srid }).ToArray()
            : [];
        return new Polygon(shell, holes) { SRID = Srid };
    }

    private static Coordinate[] ParseCoordinateRing(JsonElement ring) =>
        ring.EnumerateArray().Select(c => new Coordinate(c[0].GetDouble(), c[1].GetDouble())).ToArray();

    private static SnapshotRow ParseSnapshot(JsonElement element)
    {
        var (u, v, key, placeId) = ParseMeasurementId(element.GetProperty("id").GetString()!);
        var location = element.GetProperty("location");

        return new SnapshotRow(
            u, v, key, placeId,
            element.GetProperty("direction").GetSingle(),
            element.GetProperty("speed_ms").GetSingle(),
            element.GetProperty("suitcase_id").GetString() ?? string.Empty,
            new Point(location[1].GetDouble(), location[0].GetDouble()) { SRID = Srid },
            element.GetProperty("details").EnumerateArray().Select(d => d.GetInt16()).ToArray(),
            element.GetProperty("date").GetDateTime(),
            element.GetProperty("posted_date").GetDateTime(),
            element.GetProperty("city_id").GetInt32());
    }

    private static CciRow ParseCci(JsonElement element)
    {
        var (u, v, key, placeId) = ParseMeasurementId(element.GetProperty("id").GetString()!);
        var perTypes = element.GetProperty("cci_per_types");

        return new CciRow(
            u, v, key, placeId,
            element.GetProperty("date").GetDateTime(),
            element.GetProperty("posted_date").GetDateTime(),
            element.GetProperty("direction").GetSingle(),
            element.GetNullableSingle("cci"),
            element.GetNullableSingle("cci_custom"),
            element.GetProperty("has_measure").GetBoolean(),
            element.GetProperty("suitcase_ids").EnumerateArray().Select(s => s.GetString() ?? string.Empty).ToArray(),
            perTypes.GetNullableSingle("rateLeaves"),
            perTypes.GetNullableSingle("rateCigarrettes"),
            perTypes.GetNullableSingle("rateGums"),
            perTypes.GetNullableSingle("ratePapers"),
            perTypes.GetNullableSingle("rateGrits"),
            perTypes.GetNullableSingle("rateBottles"),
            perTypes.GetNullableSingle("rateExcrements"),
            perTypes.GetNullableSingle("rateGlassDebris"),
            perTypes.GetNullableSingle("rateSyringues"),
            element.GetProperty("custom_cci_per_types").GetRawText());
    }

    private static async Task<JsonDocument> ParseJsonAsync(Stream stream, CancellationToken cancellationToken)
    {
        try
        {
            return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        }
        catch (JsonException ex)
        {
            throw new ImportValidationException($"Fichier JSON invalide : {ex.Message}");
        }
    }

    private static void EnsureArray(JsonDocument document, string errorMessage)
    {
        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            throw new ImportValidationException(errorMessage);
        }
    }

    /// <summary>
    /// Convertit chaque élément via <paramref name="parse"/>, et transforme toute erreur de champ
    /// manquant/mal typé en ImportValidationException (400) plutôt que de laisser planter en 500.
    /// </summary>
    private static List<T> ParseAll<T>(IEnumerable<JsonElement> elements, Func<JsonElement, T> parse, string entityName)
    {
        var results = new List<T>();
        var index = 0;
        foreach (var element in elements)
        {
            try
            {
                results.Add(parse(element));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                throw new ImportValidationException($"{entityName} #{index} invalide : {ex.Message}");
            }

            index++;
        }

        return results;
    }

    private ValueTask<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken) =>
        dataSource.OpenConnectionAsync(cancellationToken);

    private static async Task ExecuteAsync(NpgsqlConnection connection, string sql, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// Crée (si besoin) les partitions mensuelles couvrant la plage de dates du lot importé.
    /// Idempotent : sans effet si la partition existe déjà.
    /// </summary>
    private static async Task EnsureMonthlyPartitionsAsync(NpgsqlConnection connection, string tableName, IEnumerable<DateTime> dates, CancellationToken cancellationToken)
    {
        var minDate = dates.Min();
        var maxDate = dates.Max();
        var cursor = new DateTime(minDate.Year, minDate.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(maxDate.Year, maxDate.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(1);

        while (cursor < end)
        {
            var next = cursor.AddMonths(1);
            var partitionName = $"{tableName}_{cursor:yyyy_MM}";
            var sql =
                $"""
                CREATE TABLE IF NOT EXISTS "{partitionName}" PARTITION OF "{tableName}"
                FOR VALUES FROM ('{cursor:yyyy-MM-dd}') TO ('{next:yyyy-MM-dd}');
                """;
            await ExecuteAsync(connection, sql, cancellationToken);
            cursor = next;
        }
    }
}
