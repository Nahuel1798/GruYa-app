using GruYaApi.DTOs.Response;

namespace GruYaApi.DTOs.Responses
{
  public class AuthResponse
  {
    public string Token { get; set; }
    public UserResponse Usuario { get; set; }
  }
}
