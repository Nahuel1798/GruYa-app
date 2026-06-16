using System.ComponentModel.DataAnnotations;

namespace GruYaApi.Models
{
    public class Quote
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int AssistanceId { get; set; }
        public Assistance Assistance { get; set; } = null!;

        [Required]
        public int ProviderId { get; set; }
        public User Provider { get; set; } = null!;

        [Required]
        public decimal Price { get; set; }

        [Required]
        public QuoteStatus Status { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
