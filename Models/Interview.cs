using System.ComponentModel.DataAnnotations;

namespace RecruitmentPlatform.API.Models
{
    public class Interview
    {
        public int Id { get; set; }

        public int JobApplicationId { get; set; }
        public JobApplication? JobApplication { get; set; }

        public int ScheduledByUserId { get; set; }
        public User? ScheduledByUser { get; set; }

        public DateTime InterviewDate { get; set; }

        [Required]
        [MaxLength(50)]
        public string Mode { get; set; } = string.Empty;
        // Online, Physical, Phone

        [MaxLength(500)]
        public string MeetingLink { get; set; } = string.Empty;

        [MaxLength(200)]
        public string Location { get; set; } = string.Empty;

        [Required]
        [MaxLength(30)]
        public string Status { get; set; } = "Scheduled";
        // Scheduled, Completed, Cancelled

        [MaxLength(1000)]
        public string Notes { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}