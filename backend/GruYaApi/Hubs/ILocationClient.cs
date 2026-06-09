using GruYaApi.DTOs.Requests;

namespace GruYaApi.Hubs
{
    public interface ILocationClient
    {
        Task LocationUpdated(CreateLocationRequest location);
        Task SessionNotFound();
        Task SessionEnded();
    }
}
