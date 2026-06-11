using GruYaApi.Models;

namespace GruYaApi.Hubs
{
    public interface ILocationClient
    {
        Task LocationUpdated(Location location);
        Task SessionNotFound();
        Task SessionEnded();
    }
}
