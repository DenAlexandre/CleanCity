namespace CortexiaAuth.Api.Models;

/// <summary>
/// Seuil déclenchant une alarme : si un relevé (EdgeSnapshots) détecte, en un seul passage, plus
/// de <see cref="Quantity"/> objets du type <see cref="TypeCode"/>, c'est considéré comme une
/// alarme (voir DashboardController.GetUrgentAlerts). Un seul seuil par type.
/// </summary>
public class AlarmThreshold
{
    public int Id { get; set; }
    public short TypeCode { get; set; }
    public int Quantity { get; set; }

    /// <summary>Envoie un e-mail aux destinataires configurés (AlarmEmailRecipients) quand ce seuil est dépassé.</summary>
    public bool SendEmail { get; set; }
}
