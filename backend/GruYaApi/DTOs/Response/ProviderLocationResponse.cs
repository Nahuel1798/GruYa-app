using GruYaApi.Models;

namespace GruYaApi.DTOs.Responses
{
    public class ProviderLocationResponse
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string CompanyName { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string Description { get; set; } = null!;
        public ServiceType ServiceType { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public bool IsAvailable { get; set; }
    }
}