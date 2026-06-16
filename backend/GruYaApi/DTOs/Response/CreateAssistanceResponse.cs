namespace GruYaApi.DTOs.Responses;

public class CreateAssistanceResponse
{
    public int AssistanceId { get; set; }
    public bool HasProvider { get; set; }
    public double? DistanceKm { get; set; }
    public double? EtaMinutes { get; set; }
    public string? RouteGeometry { get; set; }
}
