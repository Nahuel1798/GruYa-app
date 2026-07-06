namespace GruYaApi.Models
{
    public class Assistance
    {
        public int Id { get; set; }

        public ServiceType ServiceType { get; set; }

        public IssueType IssueType { get; set; }

        public AssistanceStatus Status { get; set; }

        public Vehicle? Vehicle { get; set; }

        public Location Origin { get; set; } = null!;
        public Location Destination { get; set; } = null!;
        
        public int ClientId { get; set; }
        public User Client { get; set; } = null!;
        public User? Provider { get; set; }

        public int? RequestedProviderProfileId { get; set; }
        public ProviderProfile? RequestedProviderProfile { get; set; }

        public double? DistanceKm { get; set; }
        public double? EtaMinutes { get; set; }
        public string? RouteGeometry { get; set; }

        /// <summary>
        /// Session ID for SignalR location tracking (format: "assistance-{id}").
        /// Set when the provider starts the trip.
        /// </summary>
        public string? TrackingSessionId { get; set; }

        // Relación con el pago
        public Payment? Payment { get; set; }
    }
}
