using System.ComponentModel.DataAnnotations;

namespace RecruitmentPlatform.API.DTOs.Candidate
{
    public class CandidateProfileRequest
    {
        [MaxLength(20)]
        public string Phone { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Location { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string Summary { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string Skills { get; set; } = string.Empty;

        public int YearsOfExperience { get; set; }
    }
}