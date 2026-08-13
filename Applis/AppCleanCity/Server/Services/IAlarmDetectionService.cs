namespace CortexiaAuth.Api.Services;

public interface IAlarmDetectionService
{
    /// <summary>
    /// Détecte les nouveaux dépassements de seuil (AlarmThresholds) parmi les relevés existants,
    /// les persiste dans Alarms (un seul enregistrement par relevé/type, jamais renotifié), et
    /// envoie un e-mail groupé aux destinataires configurés pour les seuils avec SendEmail actif.
    /// Retourne le nombre de nouvelles alarmes créées.
    /// </summary>
    Task<int> DetectAndNotifyAsync(CancellationToken cancellationToken);
}
