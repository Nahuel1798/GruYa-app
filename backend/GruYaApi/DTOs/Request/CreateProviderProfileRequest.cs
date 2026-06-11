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

        [Required]
        public CreateLocationRequest Location { get; set; } = null!;
    }
}
