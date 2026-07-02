using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GruYaApi.Models
{
    public class Vehicle
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        [Required]
        public VehicleType Type { get; set; }
        [Required]
        public string LicensePlate { get; set; } = null!;
        [Required]
        public string Brand { get; set; } = null!;
        [Required]
        public string Model { get; set; } = null!;
        [Required]
        public string Insurance { get; set; } = null!;
        [Required]
        public string Color { get; set; } = null!;
        public string? ImageUrl { get; set; }
    }
}