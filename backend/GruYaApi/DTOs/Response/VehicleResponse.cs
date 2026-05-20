using GruYaApi.Models;

namespace GruYaApi.DTOs.Response
{
    public class VehicleResponse
    {
        public int Id { get; set; }

        public VehicleType Type { get; set; }

        public string LicensePlate { get; set; } = null!;

        public string Brand { get; set; } = null!;

        public string Model { get; set; } = null!;

        public string Insurance { get; set; } = null!;

        public string Color { get; set; } = null!;
    }
}

