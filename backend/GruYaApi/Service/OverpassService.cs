using System.Globalization;
using System.Text.Json;
using GruYaApi.DTOs.Responses;

public class OverpassService
{
    private readonly HttpClient _http;

    private readonly string[] _servers =
    {
        "https://overpass-api.de/api/interpreter",
        "https://overpass.kumi.systems/api/interpreter",
        "https://overpass.openstreetmap.ru/api/interpreter"
    };

    public OverpassService(HttpClient http)
    {
        _http = http;
        _http.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<List<FuelStationDto>> GetFuelStationsAsync(
        double latitude,
        double longitude,
        int radius = 5000)
    {
        var lat = latitude.ToString(CultureInfo.InvariantCulture);
        var lon = longitude.ToString(CultureInfo.InvariantCulture);

        var query = $@"
[out:json][timeout:25];
(
  node[""amenity""=""fuel""](around:{radius},{lat},{lon});
);
out body;";

        Exception? lastException = null;

        foreach (var server in _servers)
        {
            try
            {
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("data", query)
                });

                var response = await _http.PostAsync(server, content);

                var body = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"OVERPASS SERVER: {server}");
                Console.WriteLine($"STATUS: {(int)response.StatusCode}");
                Console.WriteLine(body);

                if (!response.IsSuccessStatusCode)
                {
                    lastException = new Exception(
                        $"Servidor {server} respondió {(int)response.StatusCode}"
                    );
                    continue;
                }

                if (!body.TrimStart().StartsWith("{"))
                {
                    lastException = new Exception(
                        $"Servidor {server} devolvió una respuesta no JSON"
                    );
                    continue;
                }

                var result = JsonSerializer.Deserialize<OverpassResponse>(
                    body,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                if (result?.Elements == null)
                {
                    return new List<FuelStationDto>();
                }

                return result.Elements.Select(e => new FuelStationDto
                {
                    Id = e.Id,
                    Name = e.Tags?.GetValueOrDefault("name") ?? "Sin nombre",
                    Latitude = e.Lat,
                    Longitude = e.Lon
                }).ToList();
            }
            catch (Exception ex)
            {
                lastException = ex;
            }
        }

        throw new Exception(
            "No fue posible consultar ningún servidor Overpass.",
            lastException
        );
    }
}