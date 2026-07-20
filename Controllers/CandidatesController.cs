using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruitmentPlatform.API.Data;
using RecruitmentPlatform.API.DTOs.Candidate;
using RecruitmentPlatform.API.Helpers;
using RecruitmentPlatform.API.Models;

namespace RecruitmentPlatform.API.Controllers
{
    [ApiController]
    [Route("api/candidates")]
    [Authorize(Roles = UserRoles.Candidate)]
    public class CandidatesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public CandidatesController(
            ApplicationDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            int userId = GetCurrentUserId();

            CandidateProfile? profile = await _context.CandidateProfiles
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (profile == null)
            {
                return NotFound(new
                {
                    message = "Candidate profile not found."
                });
            }

            return Ok(MapToResponse(profile));
        }

        [HttpPost("profile")]
        public async Task<IActionResult> CreateProfile(CandidateProfileRequest request)
        {
            int userId = GetCurrentUserId();

            bool profileExists = await _context.CandidateProfiles
                .AnyAsync(c => c.UserId == userId);

            if (profileExists)
            {
                return Conflict(new
                {
                    message = "Candidate profile already exists. Use PUT to update it."
                });
            }

            CandidateProfile profile = new CandidateProfile
            {
                UserId = userId,
                Phone = request.Phone.Trim(),
                Location = request.Location.Trim(),
                Summary = request.Summary.Trim(),
                Skills = request.Skills.Trim(),
                YearsOfExperience = request.YearsOfExperience,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.CandidateProfiles.Add(profile);
            await _context.SaveChangesAsync();

            profile = await _context.CandidateProfiles
                .Include(c => c.User)
                .FirstAsync(c => c.Id == profile.Id);

            return Ok(new
            {
                message = "Candidate profile created successfully.",
                profile = MapToResponse(profile)
            });
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile(CandidateProfileRequest request)
        {
            int userId = GetCurrentUserId();

            CandidateProfile? profile = await _context.CandidateProfiles
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (profile == null)
            {
                return NotFound(new
                {
                    message = "Candidate profile not found. Create profile first."
                });
            }

            profile.Phone = request.Phone.Trim();
            profile.Location = request.Location.Trim();
            profile.Summary = request.Summary.Trim();
            profile.Skills = request.Skills.Trim();
            profile.YearsOfExperience = request.YearsOfExperience;
            profile.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Candidate profile updated successfully.",
                profile = MapToResponse(profile)
            });
        }

        [HttpPost("upload-resume")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadResume([FromForm] ResumeUploadRequest request)
        {
            IFormFile file = request.File;

            int userId = GetCurrentUserId();

            CandidateProfile? profile = await _context.CandidateProfiles
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (profile == null)
            {
                return BadRequest(new
                {
                    message = "Please create a candidate profile before uploading a resume."
                });
            }

            if (file == null || file.Length == 0)
            {
                return BadRequest(new
                {
                    message = "Please upload a valid resume file."
                });
            }

            long maxFileSize = 5 * 1024 * 1024;

            if (file.Length > maxFileSize)
            {
                return BadRequest(new
                {
                    message = "Resume file size must be less than 5 MB."
                });
            }

            string extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            string[] allowedExtensions =
            {
                ".pdf",
                ".doc",
                ".docx"
            };

            if (!allowedExtensions.Contains(extension))
            {
                return BadRequest(new
                {
                    message = "Only PDF, DOC, and DOCX resume files are allowed."
                });
            }

            string webRootPath = _environment.WebRootPath;

            if (string.IsNullOrWhiteSpace(webRootPath))
            {
                webRootPath = Path.Combine(_environment.ContentRootPath, "wwwroot");
            }

            if (!Directory.Exists(webRootPath))
            {
                Directory.CreateDirectory(webRootPath);
            }

            string resumeFolder = Path.Combine(webRootPath, "resumes");

            if (!Directory.Exists(resumeFolder))
            {
                Directory.CreateDirectory(resumeFolder);
            }

            if (!string.IsNullOrWhiteSpace(profile.ResumeUrl))
            {
                string oldRelativePath = profile.ResumeUrl.TrimStart('/')
                    .Replace('/', Path.DirectorySeparatorChar);

                string oldFilePath = Path.Combine(webRootPath, oldRelativePath);

                if (System.IO.File.Exists(oldFilePath))
                {
                    System.IO.File.Delete(oldFilePath);
                }
            }

            string fileName = $"candidate_{profile.Id}_{Guid.NewGuid():N}{extension}";
            string filePath = Path.Combine(resumeFolder, fileName);

            using (FileStream stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            profile.ResumeUrl = $"/resumes/{fileName}";
            profile.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            string fullResumeUrl = $"{Request.Scheme}://{Request.Host}{profile.ResumeUrl}";

            return Ok(new
            {
                message = "Resume uploaded successfully.",
                resumeUrl = profile.ResumeUrl,
                fullResumeUrl,
                profile = MapToResponse(profile)
            });
        }

        private int GetCurrentUserId()
        {
            string? userIdText = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdText, out int userId))
            {
                throw new UnauthorizedAccessException("Invalid user token.");
            }

            return userId;
        }

        private static CandidateProfileResponse MapToResponse(CandidateProfile profile)
        {
            return new CandidateProfileResponse
            {
                Id = profile.Id,
                UserId = profile.UserId,
                FullName = profile.User?.FullName ?? string.Empty,
                Email = profile.User?.Email ?? string.Empty,
                Phone = profile.Phone,
                Location = profile.Location,
                Summary = profile.Summary,
                Skills = profile.Skills,
                YearsOfExperience = profile.YearsOfExperience,
                ResumeUrl = profile.ResumeUrl,
                UpdatedAt = profile.UpdatedAt
            };
        }
    }
}