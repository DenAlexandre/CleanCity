using System.ComponentModel.DataAnnotations;

namespace CortexiaAuth.Api.Models;

public class ResetPasswordRequest
{
    [Required]
    public string NewPassword { get; set; } = string.Empty;
}
