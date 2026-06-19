using System.ComponentModel.DataAnnotations;

namespace GruYaApi.DTOs.Request
{
    public class FcmTokenRequest
    {
        [Required]
        [MinLength(1)]
        public string Token { get; set; } = null!;
    }
}
