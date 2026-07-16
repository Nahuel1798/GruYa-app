namespace GruYaApi.DTOs.Responses
{
    public class AuthResponse
    {
        public string Token { get; set; }
        public UserResponse User { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiresAt { get; set; }
    }
}
