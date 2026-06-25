using System.ComponentModel.DataAnnotations;

namespace GruYaApi.Models
{
    public class ProviderProfile
    {
        [Key]
        public int Id { get; set; }

        public string? CompanyName { get; set; }

        public string? Address { get; set; }

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

        // GPS actual
        public double? CurrentLatitude { get; set; }

        public double? CurrentLongitude { get; set; }

        public DateTime? LastLocationUpdate { get; set; }

        public bool IsAvailable { get; set; } = true;
    }
}
