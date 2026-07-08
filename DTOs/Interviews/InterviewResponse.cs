namespace RecruitmentPlatform.API.DTOs.Interviews
{
    public class InterviewResponse
    {
        public int Id { get; set; }

        public int JobApplicationId { get; set; }

        public int JobPostingId { get; set; }

        public string JobTitle { get; set; } = string.Empty;

        public int CandidateProfileId { get; set; }

        public string CandidateName { get; set; } = string.Empty;

        public string CandidateEmail { get; set; } = string.Empty;

        public int ScheduledByUserId { get; set; }

        public string ScheduledByName { get; set; } = string.Empty;

        public DateTime InterviewDate { get; set; }

        public string Mode { get; set; } = string.Empty;

        public string MeetingLink { get; set; } = string.Empty;

        public string Location { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string Notes { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}