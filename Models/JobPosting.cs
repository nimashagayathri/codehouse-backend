using System.ComponentModel.DataAnnotations;

namespace RecruitmentPlatform.API.Models
{
    public class JobPosting
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(3000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [MaxLength(1000)]
        public string RequiredSkills { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Location { get; set; } = string.Empty;

        public decimal Salary { get; set; }

        [MaxLength(50)]
        public string EmploymentType { get; set; } = string.Empty;

        public int RecruiterId { get; set; }
        public User? Recruiter { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}