using System.Globalization;
using System.Text;
using System.Text.Json;
using GruYaApi.DTOs.Responses;

public class OverpassService
{
    private readonly HttpClient _http;

    public OverpassService(HttpClient http)
    {
        _http = http;
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
  nwr[""amenity""=""fuel""](around:{radius},{lat},{lon});
);
out center;";

        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("data", query)
        });

        var response = await _http.PostAsync(
            "https://overpass-api.de/api/interpreter",
            content
        );

        var body = await response.Content.ReadAsStringAsync();

        Console.WriteLine("OVERPASS RESPONSE:");
        Console.WriteLine(body);

        // 🔴 1. validar HTTP
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Overpass HTTP error {(int)response.StatusCode}: {body}");
        }

        // 🔴 2. validar que sea JSON real
        var trimmed = body.TrimStart();

        if (!trimmed.StartsWith("{"))
        {
            throw new Exception("Overpass devolvió HTML o respuesta inválida:\n" + body);
        }

        // 🔴 3. deserializar seguro
        OverpassResponse? result;
        try
        {
            result = JsonSerializer.Deserialize<OverpassResponse>(body,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
        }
        catch (Exception ex)
        {
            throw new Exception("Error parseando JSON de Overpass: " + ex.Message + "\nRAW:\n" + body);
        }

        return result?.Elements?.Select(e => new FuelStationDto
        {
            Id = e.Id,
            Name = e.Tags?.GetValueOrDefault("name") ?? "Sin nombre",
            Latitude = e.Lat,
            Longitude = e.Lon
        }).ToList() ?? new();
    }
}