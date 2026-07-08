using System.ComponentModel.DataAnnotations;

namespace RecruitmentPlatform.API.Models
{
    public class JobApplication
    {
        public int Id { get; set; }

        public int JobPostingId { get; set; }
        public JobPosting? JobPosting { get; set; }

        public int CandidateProfileId { get; set; }
        public CandidateProfile? CandidateProfile { get; set; }

        [Required]
        [MaxLength(30)]
        public string Status { get; set; } = "Submitted";
        // Submitted, Reviewed, Shortlisted, Rejected, Hired

        public double AiMatchScore { get; set; }

        public DateTime AppliedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}