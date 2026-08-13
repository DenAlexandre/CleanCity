namespace CortexiaAuth.Api.Services;

/// <summary>Configuration SMTP (section "Smtp" d'appsettings). Host vide = envoi désactivé.</summary>
public class SmtpSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string FromAddress { get; set; } = "noreply@cleancity.local";
    public string FromName { get; set; } = "CleanCity";
}
