using System.ComponentModel.DataAnnotations;

namespace RecruitmentPlatform.API.DTOs.Candidate
{
    public class ResumeUploadRequest
    {
        [Required]
        public IFormFile File { get; set; } = null!;
    }
}