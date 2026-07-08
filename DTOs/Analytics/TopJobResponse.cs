namespace RecruitmentPlatform.API.DTOs.Analytics
{
    public class TopJobResponse
    {
        public int JobPostingId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Location { get; set; } = string.Empty;

        public string RecruiterName { get; set; } = string.Empty;

        public int ApplicationsCount { get; set; }

        public int ShortlistedCount { get; set; }

        public int HiredCount { get; set; }

        public double AverageAiMatchScore { get; set; }
    }
}