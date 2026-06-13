using System.ComponentModel.DataAnnotations;
using GruYaApi.Models;

namespace GruYaApi.DTOs.Requests
{
    public class UpdateProviderProfileRequest
    {
        [Required]
        public ServiceType ServiceType { get; set; }

        public Location? Location { get; set; } = null!;

        public string? CompanyName { get; set; }

        public string? Address { get; set; }

        public string? Description { get; set; }
    }
}
