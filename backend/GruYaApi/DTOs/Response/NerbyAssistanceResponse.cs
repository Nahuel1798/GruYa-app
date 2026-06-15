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
        public decimal OriginLatitude { get; set; }
        public decimal OriginLongitude { get; set; }
        public decimal DestinationLatitude { get; set; }
        public decimal DestinationLongitude { get; set; }
        public decimal DistanceKm { get; set; }
    }
}