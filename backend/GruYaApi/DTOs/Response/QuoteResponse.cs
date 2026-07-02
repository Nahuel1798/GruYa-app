using GruYaApi.Models;

namespace GruYaApi.DTOs.Responses
{
    public class QuoteResponse
    {
        public int Id { get; set; }
        public int AssistanceId { get; set; }
        public decimal Price { get; set; }
        public QuoteStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string ProviderName { get; set; } = "";
        public string? ProviderPhone { get; set; }
        public AssistanceResponse? Assistance { get; set; }
    }
}
