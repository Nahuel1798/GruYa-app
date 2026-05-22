using System.ComponentModel.DataAnnotations;

namespace GruYaApi.Models
{
    public class ServiceRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public ServiceType ServiceType { get; set; }

        [Required]
        public ServiceRequestStatus Status { get; set; }

        // [Required]
        // public Payment Payment { get; set; } = null!;

        [Required]
        public Vehicle? Vehicle { get; set; }

        [Required]
        public Location Location { get; set; } = null!;

        [Required]
        public User Client { get; set; } = null!;
        public User? Provider { get; set; }
    }
}

