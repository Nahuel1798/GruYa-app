using System;

namespace GruYaApi.DTOs.Responses
{
    /// <summary>
    /// Response DTO for when a provider starts a trip
    /// Contains only the essential information needed for SignalR tracking
    /// </summary>
    public class TripStartedResponse
    {
        /// <summary>
        /// The ID of the assistance that was started
        /// </summary>
        public int IdAssistance { get; set; }

        /// <summary>
        /// The SignalR session ID for location tracking (format: "assistance-{id}")
        /// Provider should call StartTracking(sessionId) on LocationHub.
        /// Client should call WatchSession(sessionId) on LocationHub.
        /// </summary>
        public string TrackingSessionId { get; set; } = string.Empty;
    }
}