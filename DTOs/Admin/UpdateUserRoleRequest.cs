using System.ComponentModel.DataAnnotations;

namespace RecruitmentPlatform.API.DTOs.Admin
{
    public class UpdateUserRoleRequest
    {
        [Required]
        [MaxLength(30)]
        public string Role { get; set; } = string.Empty;
    }
}