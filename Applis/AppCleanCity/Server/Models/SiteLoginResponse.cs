namespace CortexiaAuth.Api.Models;

public record SiteLoginResponse(string AccessToken, string TokenType, UserPermissions Permissions);
