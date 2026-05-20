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

        public DateTime Date { get; set; } = DateTime.Now;
    }
}