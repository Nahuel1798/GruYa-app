using GruYaApi.Models;

namespace GruYaApi.DTOs.Response
{
    public class ProviderLocationResponse
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public string Description { get; set; } = null!;
        public ServiceType ServiceType { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public bool IsAvailable { get; set; }
    }
}