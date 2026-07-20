using System.ComponentModel.DataAnnotations;

namespace RecruitmentPlatform.API.DTOs.Interviews
{
    public class ScheduleInterviewRequest
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int JobApplicationId { get; set; }

        [Required]
        public DateTime InterviewDate { get; set; }

        [Required]
        [MaxLength(50)]
        public string Mode { get; set; } = string.Empty;

        [MaxLength(500)]
        public string MeetingLink { get; set; } = string.Empty;

        [MaxLength(200)]
        public string Location { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string Notes { get; set; } = string.Empty;
    }
}