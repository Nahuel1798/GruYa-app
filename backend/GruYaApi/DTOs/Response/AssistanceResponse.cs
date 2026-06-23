using GruYaApi.DTOs.Responses;
using GruYaApi.Models;

namespace GruYaApi.DTOs.Responses
{
    public class AssistanceResponse
    {
        public int Id { get; set; }

        public ServiceType ServiceType { get; set; }
        public IssueType IssueType { get; set; }

        public AssistanceStatus Status { get; set; }

        public VehicleResponse? Vehicle { get; set; }

        public Location Origin { get; set; } = null!;
        public Location Destination { get; set; } = null!;

        public UserResponse Client { get; set; } = null!;

        public ProviderProfileResponse? ProviderProfile { get; set; }

        public bool IsDirected { get; set; }

        public double? DistanceKm { get; set; }
        public double? EtaMinutes { get; set; }
        public string? RouteGeometry { get; set; }

        /// <summary>
        /// Session ID for SignalR location tracking (format: "assistance-{id}").
        /// Provider should call StartTracking(sessionId) on LocationHub.
        /// Client should call WatchSession(sessionId) on LocationHub.
        /// </summary>
        public string? TrackingSessionId { get; set; }
    }
}
