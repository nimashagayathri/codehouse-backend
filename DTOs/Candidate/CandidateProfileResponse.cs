namespace RecruitmentPlatform.API.DTOs.Candidate
{
    public class CandidateProfileResponse
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public string Location { get; set; } = string.Empty;

        public string Summary { get; set; } = string.Empty;

        public string Skills { get; set; } = string.Empty;

        public int YearsOfExperience { get; set; }

        public string ResumeUrl { get; set; } = string.Empty;

        public DateTime UpdatedAt { get; set; }
    }
}