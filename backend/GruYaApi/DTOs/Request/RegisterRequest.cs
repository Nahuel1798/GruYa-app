using System.ComponentModel.DataAnnotations;
namespace GruYaApi.DTOs.Requests
{
    public class RegisterRequest
    {
        [Required]
        public required string Nombre { get; set; }
        [Required]
        public required string Apellido { get; set; }
        [Required]
        public required string Email { get; set; }
        [Required]
        public required string Contrasena { get; set; }
        public string? Telefono { get; set; }
        public string? Rol { get; set; }
        public string? Avatar { get; set; }
    }
}
