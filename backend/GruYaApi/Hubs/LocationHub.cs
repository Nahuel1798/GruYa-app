using GruYaApi.DTOs.Requests;
using GruYaApi.Service;
using Microsoft.AspNetCore.SignalR;

namespace GruYaApi.Hubs
{
    public class LocationHub : Hub<ILocationClient>
    {
        private readonly ISessionService _sessions;

        public LocationHub(ISessionService sessions)
        {
            _sessions = sessions;
        }

        public async Task StartTracking(string sessionId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
            await _sessions.RegisterTracker(sessionId, Context.ConnectionId);
        }

        public async Task WatchSession(string sessionId)
        {
            if (!await _sessions.Exists(sessionId))
            {
                await Clients.Caller.SessionNotFound();
                return;
            }
            await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
        }

        public async Task UpdateLocation(CreateLocationRequest location)
        {
            var sessionId = await _sessions.GetSessionByConnection(Context.ConnectionId);
            if (sessionId == null)
                return;

            await Clients.OthersInGroup(sessionId).LocationUpdated(location);
        }

        public override async Task OnDisconnectedAsync(Exception? ex)
        {
            var sessionId = await _sessions.GetSessionByConnection(Context.ConnectionId);

            if (sessionId != null)
            {
                await _sessions.UnregisterTracker(sessionId);
                await Clients.Groups(sessionId).SessionEnded();
            }
            await base.OnDisconnectedAsync(ex);
        }
    }
}
