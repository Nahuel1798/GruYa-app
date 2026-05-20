using GruYaApi.Models;

namespace GruYaApi.DTOs.Response
{
    public class ServiceRequestResponse
    {
        public int Id { get; set; }

        public ServiceType ServiceType { get; set; }

        public ServiceRequestStatus Status { get; set; }

        public Payment Payment { get; set; } = null!;

        public Vehicle? Vehicle { get; set; }

        public Location Location { get; set; } = null!;

        public User Client { get; set; } = null!;

        public User? Provider { get; set; }
    }
}

