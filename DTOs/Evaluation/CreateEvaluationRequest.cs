using System.ComponentModel.DataAnnotations;

namespace RecruitmentPlatform.API.DTOs.Evaluations
{
    public class CreateEvaluationRequest
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int JobApplicationId { get; set; }

        [Range(0, 10)]
        public int TechnicalScore { get; set; }

        [Range(0, 10)]
        public int CommunicationScore { get; set; }

        [Range(0, 10)]
        public int ExperienceScore { get; set; }

        [MaxLength(2000)]
        public string Feedback { get; set; } = string.Empty;

        [Required]
        [MaxLength(30)]
        public string Decision { get; set; } = string.Empty;
    }
}