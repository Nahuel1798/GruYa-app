using System.Collections.Generic;

namespace GruYaApi.DTOs.Responses
{
    public class RouteLegResponse
    {
        public double DistanceKm { get; set; }
        public double EtaMinutes { get; set; }
        public string GeometryJson { get; set; } = string.Empty;
        public List<string> Instructions { get; set; } = new();
    }

    public class AssistanceRouteResponse
    {
        public RouteLegResponse ProviderToOrigin { get; set; } = new();
        public RouteLegResponse OriginToDestination { get; set; } = new();
        public RouteLegResponse? ProviderToDestination { get; set; }
    }
}
