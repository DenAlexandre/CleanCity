namespace CortexiaAuth.Api.Models;

public record AppUserSummary(string Username, string Email, string CortexiaUsername, int RoleId, string RoleName, UserPermissions Permissions, DateTime CreatedAtUtc);
