using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruitmentPlatform.API.Data;
using RecruitmentPlatform.API.DTOs.Applications;
using RecruitmentPlatform.API.Helpers;
using RecruitmentPlatform.API.Models;
using RecruitmentPlatform.API.Services;

namespace RecruitmentPlatform.API.Controllers
{
    [ApiController]
    [Route("api/applications")]
    public class ApplicationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly AiMatchingService _aiMatchingService;

        public ApplicationsController(
            ApplicationDbContext context,
            AiMatchingService aiMatchingService)
        {
            _context = context;
            _aiMatchingService = aiMatchingService;
        }

        [Authorize(Roles = UserRoles.Candidate)]
        [HttpPost("apply")]
        public async Task<IActionResult> ApplyForJob(ApplyJobRequest request)
        {
            int userId = GetCurrentUserId();

            CandidateProfile? candidateProfile = await _context.CandidateProfiles
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (candidateProfile == null)
            {
                return BadRequest(new
                {
                    message = "Please create a candidate profile before applying for jobs."
                });
            }

            JobPosting? job = await _context.JobPostings
                .FirstOrDefaultAsync(j => j.Id == request.JobPostingId && j.IsActive);

            if (job == null)
            {
                return NotFound(new
                {
                    message = "Job posting not found or inactive."
                });
            }

            bool alreadyApplied = await _context.JobApplications
                .AnyAsync(a =>
                    a.JobPostingId == request.JobPostingId &&
                    a.CandidateProfileId == candidateProfile.Id);

            if (alreadyApplied)
            {
                return Conflict(new
                {
                    message = "You have already applied for this job."
                });
            }

            double matchScore = _aiMatchingService.CalculateMatchScore(
                candidateProfile.Skills,
                job.RequiredSkills);

            JobApplication application = new JobApplication
            {
                JobPostingId = job.Id,
                CandidateProfileId = candidateProfile.Id,
                Status = ApplicationStatuses.Submitted,
                AiMatchScore = matchScore,
                AppliedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.JobApplications.Add(application);
            await _context.SaveChangesAsync();

            application = await _context.JobApplications
                .Include(a => a.JobPosting)
                .ThenInclude(j => j!.Recruiter)
                .Include(a => a.CandidateProfile)
                .ThenInclude(c => c!.User)
                .FirstAsync(a => a.Id == application.Id);

            return Ok(new
            {
                message = "Application submitted successfully.",
                applicationId = application.Id,
                aiMatchScore = application.AiMatchScore,
                application = MapToResponse(application)
            });
        }

        [Authorize(Roles = UserRoles.Candidate)]
        [HttpGet("my-applications")]
        public async Task<IActionResult> GetMyApplications()
        {
            int userId = GetCurrentUserId();

            CandidateProfile? candidateProfile = await _context.CandidateProfiles
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (candidateProfile == null)
            {
                return NotFound(new
                {
                    message = "Candidate profile not found."
                });
            }

            List<JobApplication> applications = await _context.JobApplications
                .Include(a => a.JobPosting)
                .ThenInclude(j => j!.Recruiter)
                .Include(a => a.CandidateProfile)
                .ThenInclude(c => c!.User)
                .Where(a => a.CandidateProfileId == candidateProfile.Id)
                .OrderByDescending(a => a.AppliedAt)
                .ToListAsync();

            return Ok(applications.Select(MapToResponse));
        }

        [Authorize(Roles = "Recruiter,HiringManager,Admin")]
        [HttpGet("job/{jobId:int}")]
        public async Task<IActionResult> GetApplicationsByJob(int jobId)
        {
            int userId = GetCurrentUserId();

            JobPosting? job = await _context.JobPostings
                .FirstOrDefaultAsync(j => j.Id == jobId);

            if (job == null)
            {
                return NotFound(new
                {
                    message = "Job not found."
                });
            }

            if (!CanAccessJob(job, userId))
            {
                return Forbid();
            }

            List<JobApplication> applications = await _context.JobApplications
                .Include(a => a.JobPosting)
                .ThenInclude(j => j!.Recruiter)
                .Include(a => a.CandidateProfile)
                .ThenInclude(c => c!.User)
                .Where(a => a.JobPostingId == jobId)
                .OrderByDescending(a => a.AppliedAt)
                .ToListAsync();

            return Ok(applications.Select(MapToResponse));
        }

        [Authorize]
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetApplicationById(int id)
        {
            int userId = GetCurrentUserId();

            JobApplication? application = await _context.JobApplications
                .Include(a => a.JobPosting)
                .ThenInclude(j => j!.Recruiter)
                .Include(a => a.CandidateProfile)
                .ThenInclude(c => c!.User)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (application == null)
            {
                return NotFound(new
                {
                    message = "Application not found."
                });
            }

            if (!CanAccessApplication(application, userId))
            {
                return Forbid();
            }

            return Ok(MapToResponse(application));
        }

        [Authorize(Roles = "Recruiter,HiringManager,Admin")]
        [HttpPut("{id:int}/status")]
        public async Task<IActionResult> UpdateApplicationStatus(
            int id,
            UpdateApplicationStatusRequest request)
        {
            int userId = GetCurrentUserId();

            string? normalizedStatus = ApplicationStatuses.Normalize(request.Status);

            if (normalizedStatus == null)
            {
                return BadRequest(new
                {
                    message = "Invalid status. Allowed statuses are Submitted, Reviewed, Shortlisted, Rejected, Hired."
                });
            }

            JobApplication? application = await _context.JobApplications
                .Include(a => a.JobPosting)
                .ThenInclude(j => j!.Recruiter)
                .Include(a => a.CandidateProfile)
                .ThenInclude(c => c!.User)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (application == null)
            {
                return NotFound(new
                {
                    message = "Application not found."
                });
            }

            if (!CanManageApplication(application, userId))
            {
                return Forbid();
            }

            application.Status = normalizedStatus;
            application.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Application status updated successfully.",
                application = MapToResponse(application)
            });
        }

        [Authorize(Roles = "Recruiter,HiringManager,Admin")]
        [HttpGet("ranked/{jobId:int}")]
        public async Task<IActionResult> GetRankedApplications(int jobId)
        {
            int userId = GetCurrentUserId();

            JobPosting? job = await _context.JobPostings
                .FirstOrDefaultAsync(j => j.Id == jobId);

            if (job == null)
            {
                return NotFound(new
                {
                    message = "Job not found."
                });
            }

            if (!CanAccessJob(job, userId))
            {
                return Forbid();
            }

            List<JobApplication> applications = await _context.JobApplications
                .Include(a => a.JobPosting)
                .ThenInclude(j => j!.Recruiter)
                .Include(a => a.CandidateProfile)
                .ThenInclude(c => c!.User)
                .Where(a => a.JobPostingId == jobId)
                .OrderByDescending(a => a.AiMatchScore)
                .ThenByDescending(a => a.AppliedAt)
                .ToListAsync();

            return Ok(applications.Select(MapToResponse));
        }

        [Authorize(Roles = UserRoles.Candidate)]
        [HttpGet("recommendations")]
        public async Task<IActionResult> GetJobRecommendations()
        {
            int userId = GetCurrentUserId();

            CandidateProfile? candidateProfile = await _context.CandidateProfiles
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (candidateProfile == null)
            {
                return BadRequest(new
                {
                    message = "Please create a candidate profile before requesting recommendations."
                });
            }

            List<int> alreadyAppliedJobIds = await _context.JobApplications
                .Where(a => a.CandidateProfileId == candidateProfile.Id)
                .Select(a => a.JobPostingId)
                .ToListAsync();

            List<JobPosting> activeJobs = await _context.JobPostings
                .Include(j => j.Recruiter)
                .Where(j => j.IsActive && !alreadyAppliedJobIds.Contains(j.Id))
                .ToListAsync();

            List<JobRecommendationResponse> recommendations = activeJobs
                .Select(job => new JobRecommendationResponse
                {
                    JobPostingId = job.Id,
                    Title = job.Title,
                    Description = job.Description,
                    RequiredSkills = job.RequiredSkills,
                    Location = job.Location,
                    Salary = job.Salary,
                    EmploymentType = job.EmploymentType,
                    RecruiterName = job.Recruiter?.FullName ?? string.Empty,
                    MatchScore = _aiMatchingService.CalculateMatchScore(
                        candidateProfile.Skills,
                        job.RequiredSkills),
                    RecommendationReason = "Recommended based on matching candidate skills with required job skills."
                })
                .OrderByDescending(r => r.MatchScore)
                .ToList();

            return Ok(recommendations);
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

        private bool CanAccessJob(JobPosting job, int userId)
        {
            if (User.IsInRole(UserRoles.Admin) || User.IsInRole(UserRoles.HiringManager))
                return true;

            if (User.IsInRole(UserRoles.Recruiter))
                return job.RecruiterId == userId;

            return false;
        }

        private bool CanAccessApplication(JobApplication application, int userId)
        {
            if (User.IsInRole(UserRoles.Admin) || User.IsInRole(UserRoles.HiringManager))
                return true;

            if (User.IsInRole(UserRoles.Recruiter))
                return application.JobPosting?.RecruiterId == userId;

            if (User.IsInRole(UserRoles.Candidate))
                return application.CandidateProfile?.UserId == userId;

            return false;
        }

        private bool CanManageApplication(JobApplication application, int userId)
        {
            if (User.IsInRole(UserRoles.Admin) || User.IsInRole(UserRoles.HiringManager))
                return true;

            if (User.IsInRole(UserRoles.Recruiter))
                return application.JobPosting?.RecruiterId == userId;

            return false;
        }

        private static ApplicationResponse MapToResponse(JobApplication application)
        {
            return new ApplicationResponse
            {
                Id = application.Id,
                JobPostingId = application.JobPostingId,
                JobTitle = application.JobPosting?.Title ?? string.Empty,
                JobLocation = application.JobPosting?.Location ?? string.Empty,
                CandidateProfileId = application.CandidateProfileId,
                CandidateUserId = application.CandidateProfile?.UserId ?? 0,
                CandidateName = application.CandidateProfile?.User?.FullName ?? string.Empty,
                CandidateEmail = application.CandidateProfile?.User?.Email ?? string.Empty,
                CandidateSkills = application.CandidateProfile?.Skills ?? string.Empty,
                YearsOfExperience = application.CandidateProfile?.YearsOfExperience ?? 0,
                Status = application.Status,
                AiMatchScore = application.AiMatchScore,
                AppliedAt = application.AppliedAt,
                UpdatedAt = application.UpdatedAt
            };
        }
    }
}