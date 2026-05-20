using System.ComponentModel.DataAnnotations;

namespace GruYaApi.Models
{
    public class Location
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public decimal Latitude { get; set; }

        [Required]
        public decimal Longitude { get; set; }

        [Required]
        public string Address { get; set; } = null!;
    }
}