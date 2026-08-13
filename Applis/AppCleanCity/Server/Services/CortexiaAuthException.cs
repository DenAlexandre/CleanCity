using System.Net;

namespace CortexiaAuth.Api.Services;

public class CortexiaAuthException(HttpStatusCode statusCode, string responseBody)
    : Exception($"Cortexia a répondu {(int)statusCode}: {responseBody}")
{
    public HttpStatusCode StatusCode { get; } = statusCode;
    public string ResponseBody { get; } = responseBody;
}
