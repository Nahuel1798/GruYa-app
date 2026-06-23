using System.ComponentModel.DataAnnotations;

namespace GruYaApi.DTOs.Request
{
    public class UpdatePasswordRequest
    {
        [Required]
        public string Old { get; set; } = string.Empty;

        [Required]
        [MinLength(8)]
        public string New { get; set; } = string.Empty;
    }
}
