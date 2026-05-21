using System.ComponentModel.DataAnnotations;

namespace GruYaApi.DTOs.Requests
{
    public class UpdateLocationRequest
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public decimal Latitude { get; set; }

        [Required]
        public decimal Longitude { get; set; }

        [Required]
        public string Address { get; set; } = null!;
    }
}
