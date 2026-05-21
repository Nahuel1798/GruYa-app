using GruYaApi.Models;

namespace GruYaApi.DTOs.Responses
{
    public class PaymentResponse
    {
        public int Id { get; set; }

        public decimal Amount { get; set; }

        public PaymentMethod Method { get; set; }

        public DateTime Date { get; set; }
    }
}
