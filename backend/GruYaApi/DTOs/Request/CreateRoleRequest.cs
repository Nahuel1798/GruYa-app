using System.ComponentModel.DataAnnotations;

namespace GruYaApi.DTOs.Requests
{
    public class CreateRoleRequest
    {
        [Required]
        public string Name { get; set; } = null!;
    }
}
