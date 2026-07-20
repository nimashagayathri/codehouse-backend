using System.ComponentModel.DataAnnotations;

namespace RecruitmentPlatform.API.DTOs.Interviews
{
    public class UpdateInterviewStatusRequest
    {
        [Required]
        [MaxLength(30)]
        public string Status { get; set; } = string.Empty;
    }
}