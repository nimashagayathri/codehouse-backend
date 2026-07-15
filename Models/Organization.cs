using System.ComponentModel.DataAnnotations;

namespace RecruitmentPlatform.API.Models
{
    public class Organization
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }
        
        [MaxLength(50)]
        public string Domain { get; set; }
        
        [MaxLength(500)]
        public string LogoUrl { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
