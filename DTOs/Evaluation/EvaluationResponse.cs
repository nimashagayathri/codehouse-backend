namespace RecruitmentPlatform.API.DTOs.Evaluation
{
    public class EvaluationResponse
    {
    }
}
namespace RecruitmentPlatform.API.DTOs.Evaluations
{
    public class EvaluationResponse
    {
        public int Id { get; set; }

        public int JobApplicationId { get; set; }

        public int JobPostingId { get; set; }

        public string JobTitle { get; set; } = string.Empty;

        public int CandidateProfileId { get; set; }

        public string CandidateName { get; set; } = string.Empty;

        public string CandidateEmail { get; set; } = string.Empty;

        public int EvaluatorUserId { get; set; }

        public string EvaluatorName { get; set; } = string.Empty;

        public int TechnicalScore { get; set; }

        public int CommunicationScore { get; set; }

        public int ExperienceScore { get; set; }

        public double OverallScore { get; set; }

        public string Feedback { get; set; } = string.Empty;

        public string Decision { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}