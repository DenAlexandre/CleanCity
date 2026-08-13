using System.ComponentModel.DataAnnotations;

namespace CortexiaAuth.Api.Models;

public class SaveRoleRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;

    public UserPermissions Permissions { get; set; } = new();
}
