using System.ComponentModel.DataAnnotations;

namespace GruYaApi.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string FirstName { get; set; } = null!;
        [Required]
        public string LastName { get; set; } = null!;

        [Required]
        public string Email { get; set; } = null!;

        [Required]
        public string Password { get; set; } = null!;

        [Required]
        public Role Role { get; set; } = null!;
        [Required]
        public string? AvatarUrl { get; set; }
        [Required]
        public string phone { get; set; } = null!;
    }
}