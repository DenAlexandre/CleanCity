using CortexiaAuth.Api.Models;

namespace CortexiaAuth.Api.Services;

public interface IAlarmEmailSender
{
    /// <summary>
    /// Envoie un e-mail récapitulatif des alarmes fournies aux destinataires donnés.
    /// Retourne false sans lever d'exception si le SMTP n'est pas configuré ou si l'envoi échoue
    /// (un souci de messagerie ne doit jamais faire échouer le cycle d'import).
    /// </summary>
    Task<bool> SendAlarmEmailAsync(IReadOnlyList<string> recipients, IReadOnlyList<Alarm> alarms, CancellationToken cancellationToken);
}
