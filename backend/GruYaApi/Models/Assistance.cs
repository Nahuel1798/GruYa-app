namespace GruYaApi.Models
{
    public class Assistance
    {
        public int Id { get; set; }

        public ServiceType ServiceType { get; set; }

        public IssueType IssueType { get; set; }

        public AssistanceStatus Status { get; set; }

        public Vehicle? Vehicle { get; set; }

        public Location Origin { get; set; } = null!;
        public Location Destination { get; set; } = null!;

        public User Client { get; set; } = null!;
        public User? Provider { get; set; }

        public int? RequestedProviderId { get; set; }
        public User? RequestedProvider { get; set; }
    }
}
