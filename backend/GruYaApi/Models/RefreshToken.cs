using System.ComponentModel.DataAnnotations;

namespace GruYaApi.Models
{
    public class RefreshToken
    {
        [Key]
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public string TokenHash { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? RevokedAt { get; set; }
        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
        public bool IsActive => RevokedAt == null && !IsExpired;
    }
}
