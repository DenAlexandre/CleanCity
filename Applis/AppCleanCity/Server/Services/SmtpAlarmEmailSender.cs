using System.Net;
using System.Net.Mail;
using System.Text;
using CortexiaAuth.Api.Data;
using CortexiaAuth.Api.Models;
using Microsoft.Extensions.Options;

namespace CortexiaAuth.Api.Services;

public class SmtpAlarmEmailSender(IOptions<SmtpSettings> settings, ILogger<SmtpAlarmEmailSender> logger) : IAlarmEmailSender
{
    public async Task<bool> SendAlarmEmailAsync(IReadOnlyList<string> recipients, IReadOnlyList<Alarm> alarms, CancellationToken cancellationToken)
    {
        var smtp = settings.Value;
        if (string.IsNullOrWhiteSpace(smtp.Host))
        {
            logger.LogWarning("Alarme(s) détectée(s) mais SMTP non configuré (Smtp:Host vide) : e-mail non envoyé.");
            return false;
        }

        if (recipients.Count == 0 || alarms.Count == 0)
        {
            return false;
        }

        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(smtp.FromAddress, smtp.FromName),
                Subject = alarms.Count == 1
                    ? "CleanCity - Nouvelle alarme détectée"
                    : $"CleanCity - {alarms.Count} nouvelles alarmes détectées",
                Body = BuildBody(alarms),
                IsBodyHtml = false,
            };
            foreach (var recipient in recipients)
            {
                message.To.Add(recipient);
            }

            using var client = new SmtpClient(smtp.Host, smtp.Port) { EnableSsl = smtp.EnableSsl };
            if (!string.IsNullOrEmpty(smtp.Username))
            {
                client.Credentials = new NetworkCredential(smtp.Username, smtp.Password);
            }

            await client.SendMailAsync(message, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Échec de l'envoi de l'e-mail d'alarme.");
            return false;
        }
    }

    private static string BuildBody(IReadOnlyList<Alarm> alarms)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Les alarmes suivantes ont été détectées :");
        builder.AppendLine();
        foreach (var alarm in alarms)
        {
            var typeName = DetectionTypeCatalog.GetName(alarm.TypeCode);
            var location = alarm.Street ?? "rue inconnue";
            builder.AppendLine($"- {alarm.MeasuredAt:dd/MM/yyyy HH:mm} — {location} : {alarm.Count} {typeName} (seuil : {alarm.Threshold})");
        }

        return builder.ToString();
    }
}
