using System.Collections.Concurrent;

namespace GruYaApi.Service
{
    public class SessionService : ISessionService
    {
        private readonly ConcurrentDictionary<string, string> _connections = new();

        public Task RegisterTracker(string sessionId, string connectionId)
        {
            _connections[connectionId] = sessionId;
            return Task.CompletedTask;
        }

        public Task<string> GetSessionByConnection(string connectionId)
        {
            _connections.TryGetValue(connectionId, out var sessionId);
            return Task.FromResult(sessionId);
        }

        public Task UnregisterTracker(string sessionId)
        {
            var key = _connections.FirstOrDefault(x => x.Value == sessionId).Key;
            if (key != null)
                _connections.TryRemove(key, out _);
            return Task.CompletedTask;
        }

        public Task<bool> Exists(string sessionId)
        {
            return Task.FromResult(_connections.Values.Contains(sessionId));
        }
    }
}
