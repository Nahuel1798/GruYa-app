using GruYaApi.Models;

namespace GruYaApi.DTOs.Responses
{
    public class NearbyAssistanceResponse
    {
        public int Id { get; set; }
        public string ServiceType { get; set; } = "";
        public string? IssueType { get; set; }
        public string ClientName { get; set; } = "";
        public string Vehicle { get; set; } = "";
        public Location Origin { get; set; } = null!;
        public Location Destination { get; set; } = null!;
        public decimal DistanceKm { get; set; }
        public bool IsDirected { get; set; }
    }
}
