namespace RecruitmentPlatform.API.DTOs.Admin
{
    public class AuditLogResponse
    {
        public int Id { get; set; }

        public int? UserId { get; set; }

        public string UserName { get; set; } = string.Empty;

        public string UserEmail { get; set; } = string.Empty;

        public string Action { get; set; } = string.Empty;

        public string EntityName { get; set; } = string.Empty;

        public int? EntityId { get; set; }

        public string Details { get; set; } = string.Empty;

        public string IpAddress { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}