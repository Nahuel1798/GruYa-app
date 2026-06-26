using System.ComponentModel.DataAnnotations;
using GruYaApi.Models;

namespace GruYaApi.DTOs.Requests
{
    public class CreateProviderProfileRequest
    {
        [Required]
        public ServiceType ServiceType { get; set; }

        [Required]
        public string Description { get; set; } = null!;

        public string? CompanyName { get; set; }

        public string? Address { get; set; }

        [Required]
        public Location Location { get; set; } = null!;

        [Required]
        public Location CurrentLocation { get; set; } = null!;
    }
}
