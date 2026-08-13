using System.ComponentModel.DataAnnotations;

namespace CortexiaAuth.Api.Models;

public record AlarmThresholdDto(int Id, short TypeCode, string TypeName, int Quantity, bool SendEmail);

public record DetectionTypeDto(short TypeCode, string TypeName);

public class SaveAlarmThresholdRequest
{
    public short TypeCode { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    public bool SendEmail { get; set; }
}

public record AlarmEmailRecipientDto(int Id, string Email);

public class SaveAlarmEmailRecipientRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}
