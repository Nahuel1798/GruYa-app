using System.ComponentModel.DataAnnotations;
using GruYaApi.Models;

namespace GruYaApi.DTOs.Request
{
    public class CreateProviderProfileRequest
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        public ServiceType ServiceType { get; set; }

        [Required]
        public CreateLocationRequest Location { get; set; } = null!;
    }
}

