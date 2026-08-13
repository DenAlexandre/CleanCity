namespace CortexiaAuth.Api.Models;

/// <summary>Adresse e-mail notifiée quand un seuil d'alarme configuré avec SendEmail est dépassé.</summary>
public class AlarmEmailRecipient
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
}
