using System.Text.Json;

namespace GruYaApi.Services;

public class RouteInfo
{
    public double DistanceKm { get; set; }

    public double EtaMinutes { get; set; }

    public string GeometryJson { get; set; } = "";
}
public class OsrmService
{
    private readonly HttpClient _httpClient;

    public OsrmService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<RouteInfo> GetRouteInfoAsync(
    decimal originLat,
    decimal originLon,
    decimal destLat,
    decimal destLon)
{
    var url =
        $"https://router.project-osrm.org/route/v1/driving/" +
        $"{originLon},{originLat};" +
        $"{destLon},{destLat}" +
        $"?overview=full&geometries=geojson";

    var response =
        await _httpClient.GetFromJsonAsync<OsrmResponse>(url);

    if (
        response == null ||
        response.Routes.Count == 0
    )
    {
        throw new Exception("No se pudo obtener la ruta.");
    }

    var route = response.Routes[0];

    return new RouteInfo
    {
        DistanceKm = route.Distance / 1000,
        EtaMinutes = route.Duration / 60,

        GeometryJson = JsonSerializer.Serialize(
            route.Geometry.Coordinates
        )
    };
}
}