namespace CortexiaAuth.Api.Services;

public interface ICortexiaGeoService
{
    Task<CortexiaProxyResult> GetEdgesAndPlacesGeoJsonAsync(string authorizationHeader, CancellationToken cancellationToken);

    Task<CortexiaProxyResult> GetAggregatedSnapshotsAsync(DateTime start, DateTime end, string authorizationHeader, CancellationToken cancellationToken);

    Task<CortexiaProxyResult> GetEdgesAndPlacesCciAsync(DateTime start, DateTime end, string authorizationHeader, CancellationToken cancellationToken);
}
