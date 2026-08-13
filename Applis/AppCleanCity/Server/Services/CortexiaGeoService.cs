using System.Net.Http.Headers;

namespace CortexiaAuth.Api.Services;

public class CortexiaGeoService(HttpClient httpClient) : ICortexiaGeoService
{
    private const string DateFormat = "yyyy-MM-dd HH:mm:ss";

    public Task<CortexiaProxyResult> GetEdgesAndPlacesGeoJsonAsync(string authorizationHeader, CancellationToken cancellationToken) =>
        SendAsync("elastic/edges_and_places/geojson", authorizationHeader, cancellationToken);

    public Task<CortexiaProxyResult> GetAggregatedSnapshotsAsync(DateTime start, DateTime end, string authorizationHeader, CancellationToken cancellationToken) =>
        SendAsync(BuildRangeRequestUri("elastic/aggregated_snapshots", start, end), authorizationHeader, cancellationToken);

    public Task<CortexiaProxyResult> GetEdgesAndPlacesCciAsync(DateTime start, DateTime end, string authorizationHeader, CancellationToken cancellationToken) =>
        SendAsync(BuildRangeRequestUri("elastic/edges_and_places/cci", start, end), authorizationHeader, cancellationToken);

    private static string BuildRangeRequestUri(string path, DateTime start, DateTime end) =>
        $"{path}?start={Uri.EscapeDataString(start.ToString(DateFormat))}&end={Uri.EscapeDataString(end.ToString(DateFormat))}";

    private async Task<CortexiaProxyResult> SendAsync(string requestUri, string authorizationHeader, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Authorization = AuthenticationHeaderValue.Parse(authorizationHeader);

        HttpResponseMessage response;
        try
        {
            response = await httpClient.SendAsync(request, cancellationToken);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new CortexiaTimeoutException();
        }

        using (response)
        {
            var body = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            return new CortexiaProxyResult(response.StatusCode, body);
        }
    }
}
