using System.ComponentModel.DataAnnotations;
using GruYaApi.Models;

namespace GruYaApi.DTOs.Request
{
    public class UpdateVehicleRequest
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public VehicleType Type { get; set; }

        [Required]
        public string LicensePlate { get; set; } = null!;

        [Required]
        public string Brand { get; set; } = null!;

        [Required]
        public string Model { get; set; } = null!;

        [Required]
        public string Insurance { get; set; } = null!;

        [Required]
        public string Color { get; set; } = null!;
    }
}
