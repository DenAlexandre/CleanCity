using System.Net;

namespace CortexiaAuth.Api.Services;

public record CortexiaProxyResult(HttpStatusCode StatusCode, byte[] Body);
