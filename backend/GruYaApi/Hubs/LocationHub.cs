using GruYaApi.Models;
using GruYaApi.Service;
using Microsoft.AspNetCore.SignalR;

namespace GruYaApi.Hubs
{
    public class LocationHub : Hub<ILocationClient>
    {
        private readonly ISessionService _sessions;
        private readonly ILogger<LocationHub> _logger;

        public LocationHub(ISessionService sessions, ILogger<LocationHub> logger)
        {
            _sessions = sessions;
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation(
                "New WebSocket connection: ConnectionId={ConnectionId}",
                Context.ConnectionId);

            await base.OnConnectedAsync();
        }

        public async Task StartTracking(string sessionId)
        {
            _logger.LogInformation(
                "StartTracking: ConnectionId={ConnectionId}, SessionId={SessionId}",
                Context.ConnectionId, sessionId);

            await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
            await _sessions.RegisterTracker(sessionId, Context.ConnectionId);

            _logger.LogInformation(
                "StartTracking completed: ConnectionId={ConnectionId}, SessionId={SessionId}",
                Context.ConnectionId, sessionId);
        }

        public async Task WatchSession(string sessionId)
        {
            _logger.LogInformation(
                "WatchSession: ConnectionId={ConnectionId}, SessionId={SessionId}",
                Context.ConnectionId, sessionId);

            // Always add the connection to the SignalR group, regardless of whether the
            // session has already been registered by a provider. This eliminates the race
            // condition where a client watches before the provider calls StartTracking.
            await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);

            _logger.LogInformation(
                "WatchSession completed: ConnectionId={ConnectionId}, SessionId={SessionId}",
                Context.ConnectionId, sessionId);
        }

        public async Task UpdateLocation(Location location)
        {
            var sessionId = await _sessions.GetSessionByConnection(Context.ConnectionId);

            if (sessionId == null)
            {
                _logger.LogWarning(
                    "UpdateLocation: no session found for ConnectionId={ConnectionId}. Location={Lat},{Lng}",
                    Context.ConnectionId, location.Latitude, location.Longitude);
                return;
            }

            _logger.LogInformation(
                "UpdateLocation: ConnectionId={ConnectionId}, SessionId={SessionId}, Location=({Lat},{Lng})",
                Context.ConnectionId, sessionId, location.Latitude, location.Longitude);

            await Clients.OthersInGroup(sessionId).LocationUpdated(location);
        }

        public override async Task OnDisconnectedAsync(Exception? ex)
        {
            var sessionId = await _sessions.GetSessionByConnection(Context.ConnectionId);

            if (ex != null)
            {
                _logger.LogWarning(ex,
                    "WebSocket disconnected with error: ConnectionId={ConnectionId}, SessionId={SessionId}",
                    Context.ConnectionId, sessionId);
            }
            else
            {
                _logger.LogInformation(
                    "WebSocket disconnected: ConnectionId={ConnectionId}, SessionId={SessionId}",
                    Context.ConnectionId, sessionId);
            }

            if (sessionId != null)
            {
                await _sessions.UnregisterTracker(sessionId);
                await Clients.Groups(sessionId).SessionEnded();
            }

            await base.OnDisconnectedAsync(ex);
        }
    }
}
