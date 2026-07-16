using System.ComponentModel.DataAnnotations;

namespace GruYaApi.Models
{
    public class Payment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public decimal Amount { get; set; }

        [Required]
        public PaymentMethod Method { get; set; }
        public PaymentStatus Status { get; set; } 

        public DateTime Date { get; set; } = DateTime.UtcNow;

        // FK
        public int AssistanceId { get; set; }

        public Assistance Assistance { get; set; } = null!;
    }
}