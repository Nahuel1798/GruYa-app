using System.Net.Http.Json;

namespace GruYaApi.Services;

public class OsrmService
{
    private readonly HttpClient _httpClient;

    public OsrmService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<(double DistanceKm, double EtaMinutes)> GetRouteInfoAsync(
        decimal originLat,
        decimal originLon,
        decimal destLat,
        decimal destLon)
    {
        var url =
            $"https://router.project-osrm.org/route/v1/driving/" +
            $"{originLon},{originLat};" +
            $"{destLon},{destLat}" +
            "?overview=false";

        var response =
            await _httpClient.GetFromJsonAsync<OsrmResponse>(url);

        if (
            response == null ||
            response.Routes == null ||
            response.Routes.Count == 0
        )
        {
            throw new Exception("No se pudo obtener la ruta.");
        }

        return (
            response.Routes[0].Distance / 1000,
            response.Routes[0].Duration / 60
        );
    }
}