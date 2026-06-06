using System.ComponentModel.DataAnnotations;

namespace GruYaApi.DTOs.Requests
{
    public class CreateLocationRequest
    {
        [Required]
        public decimal Latitude { get; set; }

        [Required]
        public decimal Longitude { get; set; }
    }
}
