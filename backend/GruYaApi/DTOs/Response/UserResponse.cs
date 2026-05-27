using GruYaApi.Models;

namespace GruYaApi.DTOs.Responses
{
    public class UserResponse
    {
        public int Id { get; set; }

        public string FirstName { get; set; } = null!;

        public string LastName { get; set; } = null!;

        public string Email { get; set; } = null!;

        public Role Role { get; set; }

        public string? AvatarUrl { get; set; }

        public string Phone { get; set; } = null!;
    }
}
