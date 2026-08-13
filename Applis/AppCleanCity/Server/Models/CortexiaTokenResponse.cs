using System.Text.Json.Serialization;

namespace CortexiaAuth.Api.Models;

public class CortexiaTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = string.Empty;
}
