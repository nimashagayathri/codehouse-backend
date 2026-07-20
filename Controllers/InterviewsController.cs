using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruitmentPlatform.API.Data;
using RecruitmentPlatform.API.DTOs.Interviews;
using RecruitmentPlatform.API.Helpers;
using RecruitmentPlatform.API.Models;
using RecruitmentPlatform.API.Services;

namespace RecruitmentPlatform.API.Controllers
{
    [ApiController]
    [Route("api/interviews")]
    public class InterviewsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IGoogleCalendarService _calendarService; // ← Ashini's Google Calendar Service

        public InterviewsController(
            ApplicationDbContext context,
            IGoogleCalendarService calendarService) // ← Injected for Ashini
        {
            _context = context;
            _calendarService = calendarService;
        }

        [Authorize(Roles = "Recruiter,Admin")]
        [HttpPost("schedule")]
        public async Task<IActionResult> ScheduleInterview(ScheduleInterviewRequest request)
        {
            int userId = GetCurrentUserId();

            JobApplication? application = await _context.JobApplications
                .Include(a => a.JobPosting)
                .ThenInclude(j => j!.Recruiter)
                .Include(a => a.CandidateProfile)
                .ThenInclude(c => c!.User)
                .FirstOrDefaultAsync(a => a.Id == request.JobApplicationId);

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

            if (application.Status == ApplicationStatuses.Rejected ||
                application.Status == ApplicationStatuses.Hired)
            {
                return BadRequest(new
                {
                    message = "Cannot schedule interview for rejected or hired applications."
                });
            }

            string candidateEmail = application.CandidateProfile?.User?.Email ?? string.Empty;
            string candidateName = application.CandidateProfile?.User?.FullName ?? "Candidate";
            string jobTitle = application.JobPosting?.Title ?? "Position";
            string recruiterEmail = application.JobPosting?.Recruiter?.Email ?? string.Empty;

            // ✅ Ashini's Google Calendar API Integration
            var calendarResult = await _calendarService.CreateInterviewCalendarEventAsync(
                summary: $"Interview: {jobTitle} - CodeHouse",
                description: $"Interview with {candidateName} for {jobTitle}.\nNotes: {request.Notes}",
                location: string.IsNullOrWhiteSpace(request.Location) ? request.MeetingLink : request.Location,
                startDateTime: request.InterviewDate,
                durationMinutes: 60,
                candidateEmail: candidateEmail,
                recruiterEmail: recruiterEmail
            );

            string finalMeetingLink = !string.IsNullOrWhiteSpace(request.MeetingLink)
                ? request.MeetingLink.Trim()
                : (!string.IsNullOrWhiteSpace(calendarResult.MeetLink) ? calendarResult.MeetLink : "https://meet.google.com");

            Interview interview = new Interview
            {
                JobApplicationId = application.Id,
                ScheduledByUserId = userId,
                InterviewDate = request.InterviewDate,
                Mode = request.Mode.Trim(),
                MeetingLink = finalMeetingLink,
                Location = request.Location.Trim(),
                Status = InterviewStatuses.Scheduled,
                Notes = request.Notes.Trim(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Interviews.Add(interview);

            if (application.Status == ApplicationStatuses.Submitted)
            {
                application.Status = ApplicationStatuses.Shortlisted;
                application.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            interview = await _context.Interviews
                .Include(i => i.ScheduledByUser)
                .Include(i => i.JobApplication)
                .ThenInclude(a => a!.JobPosting)
                .Include(i => i.JobApplication)
                .ThenInclude(a => a!.CandidateProfile)
                .ThenInclude(c => c!.User)
                .FirstAsync(i => i.Id == interview.Id);

            var response = MapToResponse(interview);
            response.GoogleCalendarEventUrl = calendarResult.HtmlLink;

            return Ok(new
            {
                message = "Interview scheduled successfully and Google Calendar event created!",
                interview = response
            });
        }

        [Authorize]
        [HttpGet("application/{applicationId:int}")]
        public async Task<IActionResult> GetInterviewsByApplication(int applicationId)
        {
            int userId = GetCurrentUserId();

            JobApplication? application = await _context.JobApplications
                .Include(a => a.JobPosting)
                .Include(a => a.CandidateProfile)
                .FirstOrDefaultAsync(a => a.Id == applicationId);

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

            List<Interview> interviews = await _context.Interviews
                .Include(i => i.ScheduledByUser)
                .Include(i => i.JobApplication)
                .ThenInclude(a => a!.JobPosting)
                .Include(i => i.JobApplication)
                .ThenInclude(a => a!.CandidateProfile)
                .ThenInclude(c => c!.User)
                .Where(i => i.JobApplicationId == applicationId)
                .OrderByDescending(i => i.InterviewDate)
                .ToListAsync();

            return Ok(interviews.Select(MapToResponse));
        }

        [Authorize]
        [HttpGet("my-interviews")]
        public async Task<IActionResult> GetMyInterviews()
        {
            int userId = GetCurrentUserId();

            IQueryable<Interview> query = _context.Interviews
                .Include(i => i.ScheduledByUser)
                .Include(i => i.JobApplication)
                .ThenInclude(a => a!.JobPosting)
                .Include(i => i.JobApplication)
                .ThenInclude(a => a!.CandidateProfile)
                .ThenInclude(c => c!.User)
                .AsQueryable();

            if (User.IsInRole(UserRoles.Candidate))
            {
                query = query.Where(i =>
                    i.JobApplication!.CandidateProfile!.UserId == userId);
            }
            else if (User.IsInRole(UserRoles.Recruiter))
            {
                query = query.Where(i =>
                    i.JobApplication!.JobPosting!.RecruiterId == userId ||
                    i.ScheduledByUserId == userId);
            }
            else if (User.IsInRole(UserRoles.HiringManager) || User.IsInRole(UserRoles.Admin))
            {
                // Hiring manager and admin can view all interviews.
            }
            else
            {
                return Forbid();
            }

            List<Interview> interviews = await query
                .OrderByDescending(i => i.InterviewDate)
                .ToListAsync();

            return Ok(interviews.Select(MapToResponse));
        }

        [Authorize(Roles = "Recruiter,HiringManager,Admin")]
        [HttpPut("{id:int}/status")]
        public async Task<IActionResult> UpdateInterviewStatus(
            int id,
            UpdateInterviewStatusRequest request)
        {
            int userId = GetCurrentUserId();

            string? normalizedStatus = InterviewStatuses.Normalize(request.Status);

            if (normalizedStatus == null)
            {
                return BadRequest(new
                {
                    message = "Invalid status. Allowed statuses are Scheduled, Completed, Cancelled."
                });
            }

            Interview? interview = await _context.Interviews
                .Include(i => i.ScheduledByUser)
                .Include(i => i.JobApplication)
                .ThenInclude(a => a!.JobPosting)
                .Include(i => i.JobApplication)
                .ThenInclude(a => a!.CandidateProfile)
                .ThenInclude(c => c!.User)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (interview == null)
            {
                return NotFound(new
                {
                    message = "Interview not found."
                });
            }

            if (interview.JobApplication == null ||
                !CanManageApplication(interview.JobApplication, userId))
            {
                return Forbid();
            }

            interview.Status = normalizedStatus;
            interview.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Interview status updated successfully.",
                interview = MapToResponse(interview)
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

        private static InterviewResponse MapToResponse(Interview interview)
        {
            return new InterviewResponse
            {
                Id = interview.Id,
                JobApplicationId = interview.JobApplicationId,
                JobPostingId = interview.JobApplication?.JobPostingId ?? 0,
                JobTitle = interview.JobApplication?.JobPosting?.Title ?? string.Empty,
                CandidateProfileId = interview.JobApplication?.CandidateProfileId ?? 0,
                CandidateName = interview.JobApplication?.CandidateProfile?.User?.FullName ?? string.Empty,
                CandidateEmail = interview.JobApplication?.CandidateProfile?.User?.Email ?? string.Empty,
                ScheduledByUserId = interview.ScheduledByUserId,
                ScheduledByName = interview.ScheduledByUser?.FullName ?? string.Empty,
                InterviewDate = interview.InterviewDate,
                Mode = interview.Mode,
                MeetingLink = interview.MeetingLink,
                Location = interview.Location,
                Status = interview.Status,
                Notes = interview.Notes,
                CreatedAt = interview.CreatedAt,
                UpdatedAt = interview.UpdatedAt
            };
        }
    }
}