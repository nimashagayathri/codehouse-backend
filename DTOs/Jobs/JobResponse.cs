namespace RecruitmentPlatform.API.DTOs.Jobs
{
    public class JobResponse
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string RequiredSkills { get; set; } = string.Empty;
        
        public string Location { get; set; } = string.Empty;

        public decimal Salary { get; set; }

        public string EmploymentType { get; set; } = string.Empty;

        public int RecruiterId { get; set; }

        public string RecruiterName { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}