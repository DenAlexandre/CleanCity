namespace CortexiaAuth.Api.Models;

/// <summary>
/// Alarme persistée : un dépassement de seuil (AlarmThreshold) détecté sur un relevé donné
/// (EdgeSnapshots). Une seule alarme par (SnapshotId, TypeCode), créée par
/// IAlarmDetectionService à chaque cycle d'import, pour ne jamais notifier deux fois le même
/// dépassement.
/// </summary>
public class Alarm
{
    public long Id { get; set; }
    public long SnapshotId { get; set; }
    public short TypeCode { get; set; }
    public int Count { get; set; }
    public int Threshold { get; set; }
    public string? Street { get; set; }
    public DateTime MeasuredAt { get; set; }
    public bool EmailSent { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
