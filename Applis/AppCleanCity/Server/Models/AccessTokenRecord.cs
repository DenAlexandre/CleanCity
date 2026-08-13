namespace CortexiaAuth.Api.Models;

public class AccessTokenRecord
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}
