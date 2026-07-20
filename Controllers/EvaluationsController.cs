using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruitmentPlatform.API.Data;
using RecruitmentPlatform.API.DTOs.Evaluations;
using RecruitmentPlatform.API.Helpers;
using RecruitmentPlatform.API.Models;
using RecruitmentPlatform.API.Services;

namespace RecruitmentPlatform.API.Controllers
{
    [ApiController]
    [Route("api/evaluations")]
    public class EvaluationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public EvaluationsController(
            ApplicationDbContext context,
            IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [Authorize(Roles = "HiringManager,Admin,Recruiter")]
        [HttpPost]
        public async Task<IActionResult> CreateEvaluation(CreateEvaluationRequest request)
        {
            int userId = GetCurrentUserId();

            string? normalizedDecision = EvaluationDecisions.Normalize(request.Decision);

            if (normalizedDecision == null)
            {
                return BadRequest(new
                {
                    message = "Invalid decision. Allowed decisions are Pending, Recommended, Rejected, Hired."
                });
            }

            JobApplication? application = await _context.JobApplications
                .Include(a => a.JobPosting)
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

            bool evaluationExists = await _context.CandidateEvaluations
                .AnyAsync(e => e.JobApplicationId == request.JobApplicationId);

            if (evaluationExists)
            {
                return Conflict(new
                {
                    message = "Evaluation already exists for this application. Use PUT to update it."
                });
            }

            CandidateEvaluation evaluation = new CandidateEvaluation
            {
                JobApplicationId = request.JobApplicationId,
                EvaluatorUserId = userId,
                TechnicalScore = request.TechnicalScore,
                CommunicationScore = request.CommunicationScore,
                ExperienceScore = request.ExperienceScore,
                OverallScore = CalculateOverallScore(
                    request.TechnicalScore,
                    request.CommunicationScore,
                    request.ExperienceScore),
                Feedback = request.Feedback.Trim(),
                Decision = normalizedDecision,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            UpdateApplicationStatusBasedOnDecision(application, normalizedDecision);

            _context.CandidateEvaluations.Add(evaluation);
            await _context.SaveChangesAsync();

            evaluation = await _context.CandidateEvaluations
                .Include(e => e.EvaluatorUser)
                .Include(e => e.JobApplication)
                .ThenInclude(a => a!.JobPosting)
                .Include(e => e.JobApplication)
                .ThenInclude(a => a!.CandidateProfile)
                .ThenInclude(c => c!.User)
                .FirstAsync(e => e.Id == evaluation.Id);

            string candidateEmail = evaluation.JobApplication?.CandidateProfile?.User?.Email ?? "";
            if (!string.IsNullOrEmpty(candidateEmail))
            {
                _ = _emailService.SendEmailAsync(
                    candidateEmail,
                    $"Application Status Update: {evaluation.JobApplication?.JobPosting?.Title}",
                    $"Your application status has been updated to: {normalizedDecision}.\nFeedback: {request.Feedback}\nScore: {evaluation.OverallScore:F1}/100"
                );
            }

            return Ok(new
            {
                message = "Candidate evaluation created successfully.",
                evaluation = MapToResponse(evaluation)
            });
        }

        [Authorize]
        [HttpGet("application/{applicationId:int}")]
        public async Task<IActionResult> GetEvaluationByApplication(int applicationId)
        {
            int userId = GetCurrentUserId();

            CandidateEvaluation? evaluation = await _context.CandidateEvaluations
                .Include(e => e.EvaluatorUser)
                .Include(e => e.JobApplication)
                .ThenInclude(a => a!.JobPosting)
                .Include(e => e.JobApplication)
                .ThenInclude(a => a!.CandidateProfile)
                .ThenInclude(c => c!.User)
                .FirstOrDefaultAsync(e => e.JobApplicationId == applicationId);

            if (evaluation == null)
            {
                return NotFound(new
                {
                    message = "Evaluation not found for this application."
                });
            }

            if (evaluation.JobApplication == null ||
                !CanAccessApplication(evaluation.JobApplication, userId))
            {
                return Forbid();
            }

            return Ok(MapToResponse(evaluation));
        }

        [Authorize(Roles = "HiringManager,Admin,Recruiter")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateEvaluation(
            int id,
            UpdateEvaluationRequest request)
        {
            int userId = GetCurrentUserId();

            string? normalizedDecision = EvaluationDecisions.Normalize(request.Decision);

            if (normalizedDecision == null)
            {
                return BadRequest(new
                {
                    message = "Invalid decision. Allowed decisions are Pending, Recommended, Rejected, Hired."
                });
            }

            CandidateEvaluation? evaluation = await _context.CandidateEvaluations
                .Include(e => e.EvaluatorUser)
                .Include(e => e.JobApplication)
                .ThenInclude(a => a!.JobPosting)
                .Include(e => e.JobApplication)
                .ThenInclude(a => a!.CandidateProfile)
                .ThenInclude(c => c!.User)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (evaluation == null)
            {
                return NotFound(new
                {
                    message = "Evaluation not found."
                });
            }

            evaluation.EvaluatorUserId = userId;
            evaluation.TechnicalScore = request.TechnicalScore;
            evaluation.CommunicationScore = request.CommunicationScore;
            evaluation.ExperienceScore = request.ExperienceScore;
            evaluation.OverallScore = CalculateOverallScore(
                request.TechnicalScore,
                request.CommunicationScore,
                request.ExperienceScore);
            evaluation.Feedback = request.Feedback.Trim();
            evaluation.Decision = normalizedDecision;
            evaluation.UpdatedAt = DateTime.UtcNow;

            if (evaluation.JobApplication != null)
            {
                UpdateApplicationStatusBasedOnDecision(
                    evaluation.JobApplication,
                    normalizedDecision);
            }

            await _context.SaveChangesAsync();

            evaluation = await _context.CandidateEvaluations
                .Include(e => e.EvaluatorUser)
                .Include(e => e.JobApplication)
                .ThenInclude(a => a!.JobPosting)
                .Include(e => e.JobApplication)
                .ThenInclude(a => a!.CandidateProfile)
                .ThenInclude(c => c!.User)
                .FirstAsync(e => e.Id == id);

            string candidateEmail = evaluation.JobApplication?.CandidateProfile?.User?.Email ?? "";
            if (!string.IsNullOrEmpty(candidateEmail))
            {
                _ = _emailService.SendEmailAsync(
                    candidateEmail,
                    $"Application Status Update: {evaluation.JobApplication?.JobPosting?.Title}",
                    $"Your application status has been updated to: {normalizedDecision}.\nFeedback: {request.Feedback}\nScore: {evaluation.OverallScore:F1}/100"
                );
            }

            return Ok(new
            {
                message = "Candidate evaluation updated successfully.",
                evaluation = MapToResponse(evaluation)
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

        private static double CalculateOverallScore(
            int technicalScore,
            int communicationScore,
            int experienceScore)
        {
            return Math.Round((technicalScore + communicationScore + experienceScore) / 3.0, 2);
        }

        private static void UpdateApplicationStatusBasedOnDecision(
            JobApplication application,
            string decision)
        {
            if (decision == EvaluationDecisions.Recommended)
            {
                application.Status = ApplicationStatuses.Shortlisted;
            }
            else if (decision == EvaluationDecisions.Rejected)
            {
                application.Status = ApplicationStatuses.Rejected;
            }
            else if (decision == EvaluationDecisions.Hired)
            {
                application.Status = ApplicationStatuses.Hired;
            }
            else if (decision == EvaluationDecisions.Pending)
            {
                application.Status = ApplicationStatuses.Reviewed;
            }

            application.UpdatedAt = DateTime.UtcNow;
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

        private static EvaluationResponse MapToResponse(CandidateEvaluation evaluation)
        {
            return new EvaluationResponse
            {
                Id = evaluation.Id,
                JobApplicationId = evaluation.JobApplicationId,
                JobPostingId = evaluation.JobApplication?.JobPostingId ?? 0,
                JobTitle = evaluation.JobApplication?.JobPosting?.Title ?? string.Empty,
                CandidateProfileId = evaluation.JobApplication?.CandidateProfileId ?? 0,
                CandidateName = evaluation.JobApplication?.CandidateProfile?.User?.FullName ?? string.Empty,
                CandidateEmail = evaluation.JobApplication?.CandidateProfile?.User?.Email ?? string.Empty,
                EvaluatorUserId = evaluation.EvaluatorUserId,
                EvaluatorName = evaluation.EvaluatorUser?.FullName ?? string.Empty,
                TechnicalScore = evaluation.TechnicalScore,
                CommunicationScore = evaluation.CommunicationScore,
                ExperienceScore = evaluation.ExperienceScore,
                OverallScore = evaluation.OverallScore,
                Feedback = evaluation.Feedback,
                Decision = evaluation.Decision,
                CreatedAt = evaluation.CreatedAt,
                UpdatedAt = evaluation.UpdatedAt
            };
        }
    }
}