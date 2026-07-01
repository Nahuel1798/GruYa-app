namespace GruYaApi.DTOs.Responses
{
    public class RouteLegResponse
    {
        public double DistanceKm { get; set; }
        public double EtaMinutes { get; set; }
        public string GeometryJson { get; set; } = string.Empty;
    }

    public class AssistanceRouteResponse
    {
        public RouteLegResponse ProviderToOrigin { get; set; } = new();
        public RouteLegResponse OriginToDestination { get; set; } = new();
        public RouteLegResponse? ProviderToDestination { get; set; }
    }
}
