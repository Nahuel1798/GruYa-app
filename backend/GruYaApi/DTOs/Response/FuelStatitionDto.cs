using GruYaApi.Models;

namespace GruYaApi.DTOs.Responses
{
    public class FuelStationDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = "";
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
