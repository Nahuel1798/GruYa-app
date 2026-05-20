using GruYaApi.Models;

namespace GruYaApi.DTOs.Response
{
    public class ProviderProfileResponse
    {
        public int Id { get; set; }

        public UserResponse User { get; set; } = null!;

        public ServiceType ServiceType { get; set; }

        public LocationResponse Location { get; set; } = null!;

        public bool IsAvailable { get; set; }
    }
}

