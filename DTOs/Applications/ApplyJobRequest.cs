using System.ComponentModel.DataAnnotations;

namespace RecruitmentPlatform.API.DTOs.Applications
{
    public class ApplyJobRequest
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int JobPostingId { get; set; }
    }
}