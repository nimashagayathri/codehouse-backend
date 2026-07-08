using System.ComponentModel.DataAnnotations;

namespace RecruitmentPlatform.API.DTOs.Jobs
{
    public class UpdateJobRequest
    {
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

        public bool IsActive { get; set; } = true;
    }
}