using System.ComponentModel.DataAnnotations;

namespace RecruitmentPlatform.API.Models
{
    public class CandidateProfile
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; }

        [MaxLength(20)]
        public string Phone { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Location { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string Summary { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string Skills { get; set; } = string.Empty;

        public int YearsOfExperience { get; set; }

        [MaxLength(500)]
        public string ResumeUrl { get; set; } = string.Empty;

        public string? ResumeText { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}