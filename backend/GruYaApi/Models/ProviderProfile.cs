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
        public ServiceType ServiceType { get; set; }
        [Required]
        public Location Location { get; set; } = null!;
        [Required]
        public bool IsAvailable { get; set; }
    }
}