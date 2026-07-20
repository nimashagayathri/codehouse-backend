using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruitmentPlatform.API.Data;
using RecruitmentPlatform.API.DTOs.Analytics;
using RecruitmentPlatform.API.Helpers;
using RecruitmentPlatform.API.Models;

namespace RecruitmentPlatform.API.Controllers
{
    [ApiController]
    [Route("api/analytics")]
    [Authorize(Roles = "Recruiter,HiringManager,Admin")]
    public class AnalyticsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AnalyticsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            int userId = GetCurrentUserId();
            bool hasFullAccess = HasFullAnalyticsAccess();

            IQueryable<JobPosting> jobsQuery = GetAccessibleJobsQuery(userId);
            IQueryable<JobApplication> applicationsQuery = GetAccessibleApplicationsQuery(userId);
            IQueryable<Interview> interviewsQuery = GetAccessibleInterviewsQuery(userId);
            IQueryable<CandidateEvaluation> evaluationsQuery = GetAccessibleEvaluationsQuery(userId);

            double averageAiScore = await applicationsQuery
                .AverageAsync(a => (double?)a.AiMatchScore) ?? 0;

            double averageEvaluationScore = await evaluationsQuery
                .AverageAsync(e => (double?)e.OverallScore) ?? 0;

            AnalyticsSummaryResponse response = new AnalyticsSummaryResponse
            {
                TotalUsers = hasFullAccess
                    ? await _context.Users.CountAsync()
                    : await applicationsQuery.Select(a => a.CandidateProfileId).Distinct().CountAsync(),

                TotalCandidates = hasFullAccess
                    ? await _context.CandidateProfiles.CountAsync()
                    : await applicationsQuery.Select(a => a.CandidateProfileId).Distinct().CountAsync(),

                TotalRecruiters = hasFullAccess
                    ? await _context.Users.CountAsync(u => u.Role == UserRoles.Recruiter)
                    : User.IsInRole(UserRoles.Recruiter) ? 1 : 0,

                TotalHiringManagers = hasFullAccess
                    ? await _context.Users.CountAsync(u => u.Role == UserRoles.HiringManager)
                    : 0,

                TotalAdmins = hasFullAccess
                    ? await _context.Users.CountAsync(u => u.Role == UserRoles.Admin)
                    : 0,

                TotalJobs = await jobsQuery.CountAsync(),

                ActiveJobs = await jobsQuery.CountAsync(j => j.IsActive),

                TotalApplications = await applicationsQuery.CountAsync(),

                SubmittedApplications = await applicationsQuery
                    .CountAsync(a => a.Status == ApplicationStatuses.Submitted),

                ReviewedApplications = await applicationsQuery
                    .CountAsync(a => a.Status == ApplicationStatuses.Reviewed),

                ShortlistedApplications = await applicationsQuery
                    .CountAsync(a => a.Status == ApplicationStatuses.Shortlisted),

                RejectedApplications = await applicationsQuery
                    .CountAsync(a => a.Status == ApplicationStatuses.Rejected),

                HiredApplications = await applicationsQuery
                    .CountAsync(a => a.Status == ApplicationStatuses.Hired),

                TotalInterviews = await interviewsQuery.CountAsync(),

                ScheduledInterviews = await interviewsQuery
                    .CountAsync(i => i.Status == InterviewStatuses.Scheduled),

                CompletedInterviews = await interviewsQuery
                    .CountAsync(i => i.Status == InterviewStatuses.Completed),

                CancelledInterviews = await interviewsQuery
                    .CountAsync(i => i.Status == InterviewStatuses.Cancelled),

                TotalEvaluations = await evaluationsQuery.CountAsync(),

                AverageAiMatchScore = Math.Round(averageAiScore, 2),

                AverageEvaluationScore = Math.Round(averageEvaluationScore, 2)
            };

            return Ok(response);
        }

        [HttpGet("applications-by-status")]
        public async Task<IActionResult> GetApplicationsByStatus()
        {
            int userId = GetCurrentUserId();

            IQueryable<JobApplication> applicationsQuery = GetAccessibleApplicationsQuery(userId);

            List<ApplicationStatusCountResponse> groupedStatuses = await applicationsQuery
                .GroupBy(a => a.Status)
                .Select(g => new ApplicationStatusCountResponse
                {
                    Status = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            List<ApplicationStatusCountResponse> response = ApplicationStatuses.AllStatuses
                .Select(status => new ApplicationStatusCountResponse
                {
                    Status = status,
                    Count = groupedStatuses
                        .FirstOrDefault(g => g.Status == status)?.Count ?? 0
                })
                .ToList();

            return Ok(response);
        }

        [HttpGet("top-jobs")]
        public async Task<IActionResult> GetTopJobs([FromQuery] int limit = 5)
        {
            int userId = GetCurrentUserId();

            if (limit <= 0)
            {
                limit = 5;
            }

            IQueryable<JobPosting> jobsQuery = GetAccessibleJobsQuery(userId);

            List<JobPosting> jobs = await jobsQuery
                .Include(j => j.Recruiter)
                .ToListAsync();

            List<int> jobIds = jobs.Select(j => j.Id).ToList();

            List<JobApplication> applications = await _context.JobApplications
                .Where(a => jobIds.Contains(a.JobPostingId))
                .ToListAsync();

            List<TopJobResponse> response = jobs
                .Select(job =>
                {
                    List<JobApplication> jobApplications = applications
                        .Where(a => a.JobPostingId == job.Id)
                        .ToList();

                    return new TopJobResponse
                    {
                        JobPostingId = job.Id,
                        Title = job.Title,
                        Location = job.Location,
                        RecruiterName = job.Recruiter?.FullName ?? string.Empty,
                        ApplicationsCount = jobApplications.Count,
                        ShortlistedCount = jobApplications
                            .Count(a => a.Status == ApplicationStatuses.Shortlisted),
                        HiredCount = jobApplications
                            .Count(a => a.Status == ApplicationStatuses.Hired),
                        AverageAiMatchScore = jobApplications.Any()
                            ? Math.Round(jobApplications.Average(a => a.AiMatchScore), 2)
                            : 0
                    };
                })
                .OrderByDescending(j => j.ApplicationsCount)
                .ThenByDescending(j => j.AverageAiMatchScore)
                .Take(limit)
                .ToList();

            return Ok(response);
        }

        [HttpGet("hiring-trends")]
        public async Task<IActionResult> GetHiringTrends([FromQuery] int? year)
        {
            int userId = GetCurrentUserId();

            int selectedYear = year ?? DateTime.UtcNow.Year;

            IQueryable<JobApplication> applicationsQuery = GetAccessibleApplicationsQuery(userId);

            List<JobApplication> applications = await applicationsQuery
                .Where(a => a.AppliedAt.Year == selectedYear || a.UpdatedAt.Year == selectedYear)
                .ToListAsync();

            List<HiringTrendResponse> response = Enumerable.Range(1, 12)
                .Select(month => new HiringTrendResponse
                {
                    Year = selectedYear,
                    Month = month,
                    MonthName = CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(month),
                    ApplicationsCount = applications.Count(a =>
                        a.AppliedAt.Year == selectedYear &&
                        a.AppliedAt.Month == month),
                    HiredCount = applications.Count(a =>
                        a.Status == ApplicationStatuses.Hired &&
                        a.UpdatedAt.Year == selectedYear &&
                        a.UpdatedAt.Month == month)
                })
                .ToList();

            return Ok(response);
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

        private bool HasFullAnalyticsAccess()
        {
            return User.IsInRole(UserRoles.Admin) ||
                   User.IsInRole(UserRoles.HiringManager);
        }

        private IQueryable<JobPosting> GetAccessibleJobsQuery(int userId)
        {
            IQueryable<JobPosting> query = _context.JobPostings;

            if (User.IsInRole(UserRoles.Admin) || User.IsInRole(UserRoles.HiringManager))
            {
                return query;
            }

            if (User.IsInRole(UserRoles.Recruiter))
            {
                return query.Where(j => j.RecruiterId == userId);
            }

            return query.Where(j => false);
        }

        private IQueryable<JobApplication> GetAccessibleApplicationsQuery(int userId)
        {
            IQueryable<JobApplication> query = _context.JobApplications;

            if (User.IsInRole(UserRoles.Admin) || User.IsInRole(UserRoles.HiringManager))
            {
                return query;
            }

            if (User.IsInRole(UserRoles.Recruiter))
            {
                return query.Where(a => a.JobPosting != null &&
                                        a.JobPosting.RecruiterId == userId);
            }

            return query.Where(a => false);
        }

        private IQueryable<Interview> GetAccessibleInterviewsQuery(int userId)
        {
            IQueryable<Interview> query = _context.Interviews;

            if (User.IsInRole(UserRoles.Admin) || User.IsInRole(UserRoles.HiringManager))
            {
                return query;
            }

            if (User.IsInRole(UserRoles.Recruiter))
            {
                return query.Where(i =>
                    i.ScheduledByUserId == userId ||
                    i.JobApplication != null &&
                    i.JobApplication.JobPosting != null &&
                    i.JobApplication.JobPosting.RecruiterId == userId);
            }

            return query.Where(i => false);
        }

        private IQueryable<CandidateEvaluation> GetAccessibleEvaluationsQuery(int userId)
        {
            IQueryable<CandidateEvaluation> query = _context.CandidateEvaluations;

            if (User.IsInRole(UserRoles.Admin) || User.IsInRole(UserRoles.HiringManager))
            {
                return query;
            }

            if (User.IsInRole(UserRoles.Recruiter))
            {
                return query.Where(e =>
                    e.JobApplication != null &&
                    e.JobApplication.JobPosting != null &&
                    e.JobApplication.JobPosting.RecruiterId == userId);
            }

            return query.Where(e => false);
        }
    }
}