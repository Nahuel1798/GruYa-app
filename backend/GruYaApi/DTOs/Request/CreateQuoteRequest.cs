using System.ComponentModel.DataAnnotations;

namespace GruYaApi.DTOs.Requests
{
    public class CreateQuoteRequest
    {
        [Required]
        public int AssistanceId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        public decimal Price { get; set; }
    }
}
