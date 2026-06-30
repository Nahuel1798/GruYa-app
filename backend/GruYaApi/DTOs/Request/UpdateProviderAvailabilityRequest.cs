using System.ComponentModel.DataAnnotations;

namespace GruYaApi.DTOs.Requests
{
    public class UpdateProviderAvailabilityRequest
    {
        [Required]
        public bool IsAvailable { get; set; }
    }
}