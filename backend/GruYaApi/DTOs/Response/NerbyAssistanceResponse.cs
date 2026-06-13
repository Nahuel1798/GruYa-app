using GruYaApi.Models;
namespace GruYaApi.DTOs.Response
{
    public class NerbyAssistanceResponse
    {
         public int Id { get; set; }
        public string ServiceType { get; set; } = "";
        public string? IssueType { get; set; }
        public string ClientName { get; set; } = "";
        public string Vehicle { get; set; } = "";
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public decimal DistanceKm { get; set; }
    }
}