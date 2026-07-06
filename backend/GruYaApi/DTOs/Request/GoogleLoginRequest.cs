namespace GruYaApi.DTOs.Request;
public class GoogleLoginRequest
{
    public string IdToken { get; set; } = "";
    public string? FcmToken { get; set; }
}