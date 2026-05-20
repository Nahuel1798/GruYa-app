using System.ComponentModel.DataAnnotations;

namespace GruYaApi.Models
{
    public class CreateUserRequest
    {
        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        public Role Role { get; set; }

        [Required]
        public IFormFile AvatarFile { get; set; }

        [Required]
        public string Phone { get; set; }
    }
}

