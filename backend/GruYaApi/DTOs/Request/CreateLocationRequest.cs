using System.ComponentModel.DataAnnotations;

namespace GruYaApi.DTOs.Request
{
    public class CreateLocationRequest
    {
        [Required]
        public decimal Latitude { get; set; }

        [Required]
        public decimal Longitude { get; set; }

        [Required]
        public string Address { get; set; } = null!;
    }
}

