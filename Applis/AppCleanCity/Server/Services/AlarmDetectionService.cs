using CortexiaAuth.Api.Models;
using Npgsql;

namespace CortexiaAuth.Api.Services;

public class AlarmDetectionService(NpgsqlDataSource dataSource, IAlarmEmailSender emailSender, ILogger<AlarmDetectionService> logger)
    : IAlarmDetectionService
{
    public async Task<int> DetectAndNotifyAsync(CancellationToken cancellationToken)
    {
        var newAlarms = await InsertNewAlarmsAsync(cancellationToken);
        if (newAlarms.Count == 0)
        {
            return 0;
        }

        var toEmail = newAlarms.Where(a => a.SendEmail).Select(a => a.Alarm).ToList();
        if (toEmail.Count > 0)
        {
            var recipients = await GetRecipientsAsync(cancellationToken);
            if (recipients.Count > 0)
            {
                var sent = await emailSender.SendAlarmEmailAsync(recipients, toEmail, cancellationToken);
                if (sent)
                {
                    await MarkEmailSentAsync(toEmail.Select(a => a.Id).ToList(), cancellationToken);
                }
            }
            else
            {
                logger.LogWarning("{Count} alarme(s) à notifier par e-mail mais aucun destinataire configuré.", toEmail.Count);
            }
        }

        return newAlarms.Count;
    }

    private async Task<List<(Alarm Alarm, bool SendEmail)>> InsertNewAlarmsAsync(CancellationToken cancellationToken)
    {
        const string selectSql = """
            WITH detected AS (
                SELECT s."Id" AS "SnapshotId", d.code AS "TypeCode", COUNT(*) AS "Count"
                FROM "EdgeSnapshots" s
                CROSS JOIN LATERAL unnest(s."Details") AS d(code)
                GROUP BY s."Id", d.code
            )
            SELECT
                detected."SnapshotId",
                detected."TypeCode",
                detected."Count",
                t."Quantity" AS "Threshold",
                t."SendEmail",
                s."MeasuredAt",
                COALESCE(re."Name", p."Name") AS "Street"
            FROM detected
            JOIN "AlarmThresholds" t ON t."TypeCode" = detected."TypeCode" AND detected."Count" >= t."Quantity"
            JOIN "EdgeSnapshots" s ON s."Id" = detected."SnapshotId"
            LEFT JOIN "RoadEdges" re ON s."EdgeU" = re."U" AND s."EdgeV" = re."V" AND s."EdgeKey" = re."Key"
            LEFT JOIN "Places" p ON s."PlaceId" = p."Id"
            LEFT JOIN "Alarms" a ON a."SnapshotId" = detected."SnapshotId" AND a."TypeCode" = detected."TypeCode"
            WHERE a."Id" IS NULL
            """;

        var candidates = new List<(Alarm Alarm, bool SendEmail)>();
        await using (var connection = await dataSource.OpenConnectionAsync(cancellationToken))
        await using (var command = new NpgsqlCommand(selectSql, connection))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                var alarm = new Alarm
                {
                    SnapshotId = reader.GetInt64(0),
                    TypeCode = reader.GetInt16(1),
                    Count = (int)reader.GetInt64(2),
                    Threshold = reader.GetInt32(3),
                    MeasuredAt = reader.GetDateTime(5),
                    Street = reader.IsDBNull(6) ? null : reader.GetString(6),
                    CreatedAtUtc = DateTime.UtcNow,
                };
                candidates.Add((alarm, reader.GetBoolean(4)));
            }
        }

        if (candidates.Count == 0)
        {
            return candidates;
        }

        const string insertSql = """
            INSERT INTO "Alarms" ("SnapshotId", "TypeCode", "Count", "Threshold", "Street", "MeasuredAt", "EmailSent", "CreatedAtUtc")
            VALUES (@snapshotId, @typeCode, @count, @threshold, @street, @measuredAt, false, @createdAtUtc)
            ON CONFLICT ("SnapshotId", "TypeCode") DO NOTHING
            RETURNING "Id"
            """;

        await using var insertConnection = await dataSource.OpenConnectionAsync(cancellationToken);
        foreach (var (alarm, _) in candidates)
        {
            await using var command = new NpgsqlCommand(insertSql, insertConnection);
            command.Parameters.AddWithValue("snapshotId", alarm.SnapshotId);
            command.Parameters.AddWithValue("typeCode", alarm.TypeCode);
            command.Parameters.AddWithValue("count", alarm.Count);
            command.Parameters.AddWithValue("threshold", alarm.Threshold);
            command.Parameters.AddWithValue("street", (object?)alarm.Street ?? DBNull.Value);
            command.Parameters.AddWithValue("measuredAt", alarm.MeasuredAt);
            command.Parameters.AddWithValue("createdAtUtc", alarm.CreatedAtUtc);

            var id = await command.ExecuteScalarAsync(cancellationToken);
            if (id is long insertedId)
            {
                alarm.Id = insertedId;
            }
        }

        // Une course entre deux cycles concurrents peut faire perdre la ligne côté ON CONFLICT :
        // on ne garde que celles réellement insérées ici (Id renseigné).
        return candidates.Where(c => c.Alarm.Id != 0).ToList();
    }

    private async Task<List<string>> GetRecipientsAsync(CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand("""SELECT "Email" FROM "AlarmEmailRecipients" ORDER BY "Email" """, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var recipients = new List<string>();
        while (await reader.ReadAsync(cancellationToken))
        {
            recipients.Add(reader.GetString(0));
        }

        return recipients;
    }

    private async Task MarkEmailSentAsync(List<long> ids, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand("""UPDATE "Alarms" SET "EmailSent" = true WHERE "Id" = ANY(@ids)""", connection);
        command.Parameters.AddWithValue("ids", ids.ToArray());
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
