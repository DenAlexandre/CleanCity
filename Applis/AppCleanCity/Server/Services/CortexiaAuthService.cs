using System.Net.Http.Json;
using CortexiaAuth.Api.Models;

namespace CortexiaAuth.Api.Services;

public class CortexiaAuthService(HttpClient httpClient) : ICortexiaAuthService
{
    public async Task<CortexiaTokenResponse> GetAccessTokenAsync(string username, string password, CancellationToken cancellationToken)
    {
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["username"] = username,
            ["password"] = password,
        });

        using var response = await httpClient.PostAsync("login/access-token", form, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new CortexiaAuthException(response.StatusCode, body);
        }

        var token = await response.Content.ReadFromJsonAsync<CortexiaTokenResponse>(cancellationToken: cancellationToken);
        return token ?? throw new CortexiaAuthException(response.StatusCode, body);
    }
}
