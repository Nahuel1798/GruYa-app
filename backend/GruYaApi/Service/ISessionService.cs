namespace GruYaApi.Service
{
    public interface ISessionService
    {
        Task RegisterTracker(string sessionId, string connectionId);
        Task<string> GetSessionByConnection(string connectionId);
        Task UnregisterTracker(string sessionId);
        Task<bool> Exists(string sessionId);
    }
}
