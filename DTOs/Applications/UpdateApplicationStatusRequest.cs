using System.ComponentModel.DataAnnotations;

namespace RecruitmentPlatform.API.DTOs.Applications
{
    public class UpdateApplicationStatusRequest
    {
        [Required]
        [MaxLength(30)]
        public string Status { get; set; } = string.Empty;
    }
}