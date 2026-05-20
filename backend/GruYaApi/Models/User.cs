using System.ComponentModel.DataAnnotations;

namespace GruYaApi.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        public string FirstName { get; set; } = null!;

        public string LastName { get; set; } = null!;

        public string Email { get; set; } = null!;

        public string Password { get; set; } = null!;

        public Role Role { get; set; } = null!;

        public string? AvatarUrl { get; set; }

        public string Phone { get; set; } = null!;
    }
}

