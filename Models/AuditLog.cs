using System.ComponentModel.DataAnnotations;

namespace RecruitmentPlatform.API.Models
{
    public class AuditLog
    {
        public int Id { get; set; }

        public int? UserId { get; set; }
        public User? User { get; set; }

        [Required]
        [MaxLength(100)]
        public string Action { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string EntityName { get; set; } = string.Empty;

        public int? EntityId { get; set; }

        [MaxLength(2000)]
        public string Details { get; set; } = string.Empty;

        [MaxLength(100)]
        public string IpAddress { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}