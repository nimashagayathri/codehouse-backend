namespace RecruitmentPlatform.API.DTOs.Applications
{
    public class ApplicationResponse
    {
        public int Id { get; set; }

        public int JobPostingId { get; set; }

        public string JobTitle { get; set; } = string.Empty;

        public string JobLocation { get; set; } = string.Empty;

        public int CandidateProfileId { get; set; }

        public int CandidateUserId { get; set; }

        public string CandidateName { get; set; } = string.Empty;

        public string CandidateEmail { get; set; } = string.Empty;

        public string CandidateSkills { get; set; } = string.Empty;

        public int YearsOfExperience { get; set; }

        public string Status { get; set; } = string.Empty;

        public double AiMatchScore { get; set; }

        public DateTime AppliedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}