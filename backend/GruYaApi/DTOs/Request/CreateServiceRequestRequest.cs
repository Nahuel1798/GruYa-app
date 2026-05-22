using System.ComponentModel.DataAnnotations;
using GruYaApi.Models;

namespace GruYaApi.DTOs.Requests
{
    public class CreateServiceRequestRequest
    {
        [Required]
        public ServiceType ServiceType { get; set; }

        [Required]
        public int VehicleId { get; set; }

        [Required]
        public CreateLocationRequest Location { get; set; } = null!;

        [Required]
        public int ClientId { get; set; }

        public int ProviderId { get; set; }
    }
}
