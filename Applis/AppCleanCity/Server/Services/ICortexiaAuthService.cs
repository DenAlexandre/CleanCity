using CortexiaAuth.Api.Models;

namespace CortexiaAuth.Api.Services;

public interface ICortexiaAuthService
{
    Task<CortexiaTokenResponse> GetAccessTokenAsync(string username, string password, CancellationToken cancellationToken);
}
