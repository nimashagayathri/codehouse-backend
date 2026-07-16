using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruitmentPlatform.API.Data;
using RecruitmentPlatform.API.DTOs.Jobs;
using RecruitmentPlatform.API.Helpers;
using RecruitmentPlatform.API.Models;

namespace RecruitmentPlatform.API.Controllers
{
    [ApiController]
    [Route("api/jobs")]
    public class JobsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public JobsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetJobs()
        {
            List<JobPosting> jobs = await _context.JobPostings
                .Include(j => j.Recruiter)
                .Where(j => j.IsActive)
                .OrderByDescending(j => j.CreatedAt)
                .ToListAsync();

            return Ok(jobs.Select(MapToResponse));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetJobById(int id)
        {
            JobPosting? job = await _context.JobPostings
                .Include(j => j.Recruiter)
                .FirstOrDefaultAsync(j => j.Id == id);

            if (job == null)
            {
                return NotFound(new
                {
                    message = "Job not found."
                });
            }

            return Ok(MapToResponse(job));
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchJobs(
            [FromQuery] string? keyword,
            [FromQuery] string? location)
        {
            IQueryable<JobPosting> query = _context.JobPostings
                .Include(j => j.Recruiter)
                .Where(j => j.IsActive);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                string cleanKeyword = keyword.Trim().ToLower();

                query = query.Where(j =>
                    j.Title.ToLower().Contains(cleanKeyword) ||
                    j.Description.ToLower().Contains(cleanKeyword) ||
                    j.RequiredSkills.ToLower().Contains(cleanKeyword));
            }

            if (!string.IsNullOrWhiteSpace(location))
            {
                string cleanLocation = location.Trim().ToLower();

                query = query.Where(j =>
                    j.Location.ToLower().Contains(cleanLocation));
            }

            List<JobPosting> jobs = await query
                .OrderByDescending(j => j.CreatedAt)
                .ToListAsync();

            return Ok(jobs.Select(MapToResponse));
        }

        [Authorize(Roles = "Recruiter,Admin")]
        [HttpPost]
        public async Task<IActionResult> CreateJob(CreateJobRequest request)
        {
            int userId = GetCurrentUserId();

            JobPosting job = new JobPosting
            {
                Title = request.Title.Trim(),
                Description = request.Description.Trim(),
                RequiredSkills = request.RequiredSkills.Trim(),
                Location = request.Location.Trim(),
                Salary = request.Salary,
                EmploymentType = request.EmploymentType.Trim(),
                RecruiterId = userId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.JobPostings.Add(job);
            await _context.SaveChangesAsync();

            job = await _context.JobPostings
                .Include(j => j.Recruiter)
                .FirstAsync(j => j.Id == job.Id);

            return Ok(new
            {
                message = "Job posting created successfully.",
                job = MapToResponse(job)
            });
        }

        [Authorize(Roles = "Recruiter,Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateJob(int id, UpdateJobRequest request)
        {
            int userId = GetCurrentUserId();

            JobPosting? job = await _context.JobPostings
                .Include(j => j.Recruiter)
                .FirstOrDefaultAsync(j => j.Id == id);

            if (job == null)
            {
                return NotFound(new
                {
                    message = "Job not found."
                });
            }

            bool isAdmin = User.IsInRole(UserRoles.Admin);

            if (!isAdmin && job.RecruiterId != userId)
            {
                return Forbid();
            }

            job.Title = request.Title.Trim();
            job.Description = request.Description.Trim();
            job.RequiredSkills = request.RequiredSkills.Trim();
            job.Location = request.Location.Trim();
            job.Salary = request.Salary;
            job.EmploymentType = request.EmploymentType.Trim();
            job.IsActive = request.IsActive;
            job.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Job posting updated successfully.",
                job = MapToResponse(job)
            });
        }

        [Authorize(Roles = "Recruiter,Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteJob(int id)
        {
            int userId = GetCurrentUserId();

            JobPosting? job = await _context.JobPostings
                .FirstOrDefaultAsync(j => j.Id == id);

            if (job == null)
            {
                return NotFound(new
                {
                    message = "Job not found."
                });
            }

            bool isAdmin = User.IsInRole(UserRoles.Admin);

            if (!isAdmin && job.RecruiterId != userId)
            {
                return Forbid();
            }

            job.IsActive = false;
            job.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Job posting deleted successfully."
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

        private static JobResponse MapToResponse(JobPosting job)
        {
            return new JobResponse
            {
                Id = job.Id,
                Title = job.Title,
                Description = job.Description,
                RequiredSkills = job.RequiredSkills,
                Location = job.Location,
                Salary = job.Salary,
                EmploymentType = job.EmploymentType,
                RecruiterId = job.RecruiterId,
                RecruiterName = job.Recruiter?.FullName ?? string.Empty,
                IsActive = job.IsActive,
                CreatedAt = job.CreatedAt
            };
        }
    }
}