using System.ComponentModel.DataAnnotations;
using GruYaApi.Models;

namespace GruYaApi.DTOs.Requests
{
    public class CreatePaymentRequest
    {
        [Required]
        public decimal Amount { get; set; }

        [Required]
        public PaymentMethod Method { get; set; }
    }
}
