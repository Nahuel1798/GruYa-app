namespace GruYaApi.DTOs.Response
{
    public class UserResponse
    {
        public int Id { get; set; }

        public string FirstName { get; set; } = null!;

        public string LastName { get; set; } = null!;

        public string Email { get; set; } = null!;

        public RoleResponse Role { get; set; } = null!;

        public string? AvatarUrl { get; set; }

        public string Phone { get; set; } = null!;
    }
}
