using System.ComponentModel.DataAnnotations;

namespace RecruitmentPlatform.API.Models
{
    public class CandidateEvaluation
    {
        public int Id { get; set; }

        public int JobApplicationId { get; set; }
        public JobApplication? JobApplication { get; set; }

        public int EvaluatorUserId { get; set; }
        public User? EvaluatorUser { get; set; }

        [Range(0, 10)]
        public int TechnicalScore { get; set; }

        [Range(0, 10)]
        public int CommunicationScore { get; set; }

        [Range(0, 10)]
        public int ExperienceScore { get; set; }

        public double OverallScore { get; set; }

        [MaxLength(2000)]
        public string Feedback { get; set; } = string.Empty;

        [Required]
        [MaxLength(30)]
        public string Decision { get; set; } = "Pending";
        // Pending, Recommended, Rejected, Hired

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}