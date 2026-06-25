using System.Globalization;
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
    private readonly ILogger<OsrmService> _logger;

    public OsrmService(
        HttpClient httpClient,
        ILogger<OsrmService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<RouteInfo> GetRouteInfoAsync(
        decimal originLat,
        decimal originLon,
        decimal destLat,
        decimal destLon)
    {
        var url =
            $"https://router.project-osrm.org/route/v1/driving/" +
            $"{originLon.ToString(CultureInfo.InvariantCulture)}," +
            $"{originLat.ToString(CultureInfo.InvariantCulture)};" +
            $"{destLon.ToString(CultureInfo.InvariantCulture)}," +
            $"{destLat.ToString(CultureInfo.InvariantCulture)}" +
            "?overview=full&geometries=geojson";

        _logger.LogInformation(
            "Consultando OSRM. Origin=({OriginLat},{OriginLon}) Dest=({DestLat},{DestLon})",
            originLat,
            originLon,
            destLat,
            destLon);

        _logger.LogInformation("OSRM URL: {Url}", url);

        try
        {
            var httpResponse = await _httpClient.GetAsync(url);

            var content = await httpResponse.Content.ReadAsStringAsync();

            _logger.LogInformation("URL: {Url}", url);
            _logger.LogInformation("Status: {Status}", httpResponse.StatusCode);
            _logger.LogInformation("Response: {Response}", content);

            httpResponse.EnsureSuccessStatusCode();

            var response = JsonSerializer.Deserialize<OsrmResponse>(
                content,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            if (response == null)
            {
                _logger.LogError("OSRM devolvió respuesta nula");
                throw new Exception("OSRM devolvió respuesta nula");
            }

            _logger.LogInformation(
                "OSRM Status: Routes: {Count}",
                response.Routes?.Count ?? 0);

            if (response.Routes == null || response.Routes.Count == 0)
            {
                _logger.LogError("OSRM no devolvió rutas");
                throw new Exception("No se pudo obtener la ruta.");
            }

            var route = response.Routes[0];

            var geometryJson =
                JsonSerializer.Serialize(
                    route.Geometry.Coordinates);

            _logger.LogInformation(
                "Ruta obtenida. Distancia={Distance}m Duracion={Duration}s GeometriaChars={GeometryLength}",
                route.Distance,
                route.Duration,
                geometryJson.Length);

            return new RouteInfo
            {
                DistanceKm = route.Distance / 1000,
                EtaMinutes = route.Duration / 60,
                GeometryJson = geometryJson
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error consultando OSRM");

            throw;
        }
    }
}