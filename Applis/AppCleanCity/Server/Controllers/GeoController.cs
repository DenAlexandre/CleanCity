using System.Text;
using CortexiaAuth.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CortexiaAuth.Api.Controllers;

[ApiController]
[Route("api/geo")]
public class GeoController(ICortexiaGeoService geoService) : ControllerBase
{
    /// <summary>
    /// Récupère le GeoJSON edges_and_places depuis Cortexia. Nécessite un Authorization: Bearer {access_token}
    /// obtenu via /api/Auth/token.
    /// </summary>
    [HttpGet("edges-and-places")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK, "application/geo+json")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status504GatewayTimeout)]
    public Task<IActionResult> GetEdgesAndPlacesGeoJson(CancellationToken cancellationToken) =>
        ProxyAsync(
            authorizationHeader => geoService.GetEdgesAndPlacesGeoJsonAsync(authorizationHeader, cancellationToken),
            "edges_and_places.geojson",
            "application/geo+json");

    /// <summary>
    /// Récupère les snapshots agrégés Cortexia sur une plage de dates. Nécessite un Authorization: Bearer {access_token}.
    /// </summary>
    [HttpGet("aggregated-snapshots")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status504GatewayTimeout)]
    public Task<IActionResult> GetAggregatedSnapshots([FromQuery] DateTime start, [FromQuery] DateTime end, CancellationToken cancellationToken) =>
        ProxyAsync(
            authorizationHeader => geoService.GetAggregatedSnapshotsAsync(start, end, authorizationHeader, cancellationToken),
            "aggregated_snapshots.json",
            "application/json");

    /// <summary>
    /// Récupère le CCI des edges_and_places Cortexia sur une plage de dates. Nécessite un Authorization: Bearer {access_token}.
    /// </summary>
    [HttpGet("edges-and-places/cci")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status504GatewayTimeout)]
    public Task<IActionResult> GetEdgesAndPlacesCci([FromQuery] DateTime start, [FromQuery] DateTime end, CancellationToken cancellationToken) =>
        ProxyAsync(
            authorizationHeader => geoService.GetEdgesAndPlacesCciAsync(start, end, authorizationHeader, cancellationToken),
            "edges_and_places_cci.json",
            "application/json");

    private async Task<IActionResult> ProxyAsync(Func<string, Task<CortexiaProxyResult>> call, string fileName, string contentType)
    {
        var authorizationHeader = Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(authorizationHeader))
        {
            return Unauthorized(new { error = "En-tête Authorization (Bearer {access_token}) requis." });
        }

        CortexiaProxyResult result;
        try
        {
            result = await call(authorizationHeader);
        }
        catch (CortexiaTimeoutException)
        {
            return StatusCode(StatusCodes.Status504GatewayTimeout, new { error = "Timeout en attendant la réponse de Cortexia." });
        }

        var isSuccess = (int)result.StatusCode is >= 200 and <= 299;
        if (!isSuccess)
        {
            return new ContentResult
            {
                StatusCode = (int)result.StatusCode,
                Content = Encoding.UTF8.GetString(result.Body),
                ContentType = "application/json",
            };
        }

        return File(result.Body, contentType, fileName);
    }
}
