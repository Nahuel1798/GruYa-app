using System.ComponentModel.DataAnnotations;

namespace GruYaApi.Models
{
    public class ProviderProfile
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public User User { get; set; } = null!;

        [Required]
        public string Description { get; set; } = null!;

        [Required]
        public int UserId { get; set; }

        [Required]
        public ServiceType ServiceType { get; set; }

        [Required]
        public Location Location { get; set; } = null!;

        [Required]
        public bool IsAvailable { get; set; } = true;

        public string? Address { get; set; }
    }
}
