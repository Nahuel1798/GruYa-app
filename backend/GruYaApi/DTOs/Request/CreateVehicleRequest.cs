using System.ComponentModel.DataAnnotations;
using GruYaApi.Models;

namespace GruYaApi.DTOs.Request
{
    public class CreateVehicleRequest
    {
        [Required]
        public VehicleType Type { get; set; }

        [Required]
        public string LicensePlate { get; set; }

        [Required]
        public string Brand { get; set; }

        [Required]
        public string Model { get; set; }

        [Required]
        public string Insurance { get; set; }

        [Required]
        public string Color { get; set; }
    }
}

