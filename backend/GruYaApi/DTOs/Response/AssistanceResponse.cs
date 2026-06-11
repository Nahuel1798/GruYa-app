using GruYaApi.DTOs.Responses;
using GruYaApi.Models;

namespace GruYaApi.DTOs.Responses
{
    public class AssistanceResponse
    {
        public int Id { get; set; }

        public ServiceType ServiceType { get; set; }

        public AssistanceStatus Status { get; set; }

        public Vehicle? Vehicle { get; set; }

        public Location Location { get; set; } = null!;

        public UserResponse Client { get; set; } = null!;

        public UserResponse? Provider { get; set; }
    }
}
