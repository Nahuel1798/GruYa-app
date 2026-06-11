using System.ComponentModel.DataAnnotations;
using GruYaApi.Models;

namespace GruYaApi.DTOs.Requests
{
    public class UpdateProviderProfileRequest
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public ServiceType ServiceType { get; set; }

        public string? CompanyName { get; set; }

        [Required]
        public UpdateLocationRequest Location { get; set; } = null!;
    }
}
