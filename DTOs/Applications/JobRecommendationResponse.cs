namespace RecruitmentPlatform.API.DTOs.Applications
{
    public class JobRecommendationResponse
    {
        public int JobPostingId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string RequiredSkills { get; set; } = string.Empty;

        public string Location { get; set; } = string.Empty;

        public decimal Salary { get; set; }

        public string EmploymentType { get; set; } = string.Empty;

        public string RecruiterName { get; set; } = string.Empty;

        public double MatchScore { get; set; }

        public string RecommendationReason { get; set; } = string.Empty;
    }
}