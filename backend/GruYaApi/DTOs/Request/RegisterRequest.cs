using System.ComponentModel.DataAnnotations;

namespace GruYaApi.DTOs.Requests
{
    public class RegisterRequest
    {
        [Required]
        public required string FirstName { get; set; }

        [Required]
        public required string LastName { get; set; }

        [Required]
        public required string Email { get; set; }

        [Required]
        public required string Password { get; set; }

        public string? Phone { get; set; }

        public int RoleId { get; set; }
    }
}
