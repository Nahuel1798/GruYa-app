using System.ComponentModel.DataAnnotations;

namespace GruYaApi.DTOs.Request
{
    public class CreateRoleRequest
    {
        [Required]
        public string Name { get; set; } = null!;
    }
}

