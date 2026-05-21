namespace GruYaApi.DTOs.Responses
{
    public class AuthResponse
    {
        public string Token { get; set; }
        public UserResponse User { get; set; }
    }
}
