using System.ComponentModel.DataAnnotations;
using GruYaApi.Models;

namespace GruYaApi.DTOs.Requests
{
    public class CreateAssistanceRequest
    {
        [Required]
        public ServiceType ServiceType { get; set; }

        public IssueType IssueType { get; set; }

        [Required]
        public int VehicleId { get; set; }

        [Required]
        public Location Location { get; set; } = null!;

        public int? ProviderId { get; set; }
    }
}
