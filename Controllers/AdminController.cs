using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruitmentPlatform.API.Data;
using RecruitmentPlatform.API.DTOs.Admin;
using RecruitmentPlatform.API.Helpers;
using RecruitmentPlatform.API.Models;

namespace RecruitmentPlatform.API.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = UserRoles.Admin)]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers(
            [FromQuery] string? role,
            [FromQuery] string? search,
            [FromQuery] bool? isActive)
        {
            IQueryable<User> query = _context.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(role))
            {
                string? normalizedRole = UserRoles.Normalize(role);

                if (normalizedRole == null)
                {
                    return BadRequest(new
                    {
                        message = "Invalid role. Allowed roles are Candidate, Recruiter, HiringManager, Admin."
                    });
                }

                query = query.Where(u => u.Role == normalizedRole);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string cleanSearch = search.Trim().ToLower();

                query = query.Where(u =>
                    u.FullName.ToLower().Contains(cleanSearch) ||
                    u.Email.ToLower().Contains(cleanSearch));
            }

            if (isActive.HasValue)
            {
                query = query.Where(u => u.IsActive == isActive.Value);
            }

            List<User> users = await query
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            return Ok(users.Select(MapToUserResponse));
        }

        [HttpGet("users/{id:int}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            User? user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound(new
                {
                    message = "User not found."
                });
            }

            return Ok(MapToUserResponse(user));
        }

        [HttpPut("users/{id:int}/role")]
        public async Task<IActionResult> UpdateUserRole(
            int id,
            UpdateUserRoleRequest request)
        {
            int adminUserId = GetCurrentUserId();

            string? normalizedRole = UserRoles.Normalize(request.Role);

            if (normalizedRole == null)
            {
                return BadRequest(new
                {
                    message = "Invalid role. Allowed roles are Candidate, Recruiter, HiringManager, Admin."
                });
            }

            User? user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound(new
                {
                    message = "User not found."
                });
            }

            if (user.Id == adminUserId && normalizedRole != UserRoles.Admin)
            {
                return BadRequest(new
                {
                    message = "You cannot remove your own Admin role."
                });
            }

            string oldRole = user.Role;

            user.Role = normalizedRole;

            AddAuditLog(
                adminUserId,
                "UpdateUserRole",
                "User",
                user.Id,
                $"Changed user role from {oldRole} to {normalizedRole} for {user.Email}.");

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "User role updated successfully.",
                user = MapToUserResponse(user)
            });
        }

        [HttpPut("users/{id:int}/status")]
        public async Task<IActionResult> UpdateUserStatus(
            int id,
            UpdateUserStatusRequest request)
        {
            int adminUserId = GetCurrentUserId();

            User? user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound(new
                {
                    message = "User not found."
                });
            }

            if (user.Id == adminUserId && request.IsActive == false)
            {
                return BadRequest(new
                {
                    message = "You cannot deactivate your own Admin account."
                });
            }

            bool oldStatus = user.IsActive;

            user.IsActive = request.IsActive;

            AddAuditLog(
                adminUserId,
                "UpdateUserStatus",
                "User",
                user.Id,
                $"Changed user active status from {oldStatus} to {request.IsActive} for {user.Email}.");

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "User status updated successfully.",
                user = MapToUserResponse(user)
            });
        }

        [HttpDelete("users/{id:int}")]
        public async Task<IActionResult> DeactivateUser(int id)
        {
            int adminUserId = GetCurrentUserId();

            User? user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound(new
                {
                    message = "User not found."
                });
            }

            if (user.Id == adminUserId)
            {
                return BadRequest(new
                {
                    message = "You cannot deactivate your own Admin account."
                });
            }

            user.IsActive = false;

            AddAuditLog(
                adminUserId,
                "DeactivateUser",
                "User",
                user.Id,
                $"Deactivated user account {user.Email}.");

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "User deactivated successfully.",
                user = MapToUserResponse(user)
            });
        }

        [HttpGet("audit-logs")]
        public async Task<IActionResult> GetAuditLogs(
            [FromQuery] int? userId,
            [FromQuery] string? action,
            [FromQuery] int limit = 100)
        {
            if (limit <= 0)
            {
                limit = 100;
            }

            if (limit > 500)
            {
                limit = 500;
            }

            IQueryable<AuditLog> query = _context.AuditLogs
                .Include(a => a.User)
                .AsQueryable();

            if (userId.HasValue)
            {
                query = query.Where(a => a.UserId == userId.Value);
            }

            if (!string.IsNullOrWhiteSpace(action))
            {
                string cleanAction = action.Trim().ToLower();

                query = query.Where(a =>
                    a.Action.ToLower().Contains(cleanAction));
            }

            List<AuditLog> logs = await query
                .OrderByDescending(a => a.CreatedAt)
                .Take(limit)
                .ToListAsync();

            return Ok(logs.Select(MapToAuditLogResponse));
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

        private void AddAuditLog(
            int adminUserId,
            string action,
            string entityName,
            int? entityId,
            string details)
        {
            string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;

            AuditLog auditLog = new AuditLog
            {
                UserId = adminUserId,
                Action = action,
                EntityName = entityName,
                EntityId = entityId,
                Details = details,
                IpAddress = ipAddress,
                CreatedAt = DateTime.UtcNow
            };

            _context.AuditLogs.Add(auditLog);
        }

        private static UserAdminResponse MapToUserResponse(User user)
        {
            return new UserAdminResponse
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt
            };
        }

        private static AuditLogResponse MapToAuditLogResponse(AuditLog auditLog)
        {
            return new AuditLogResponse
            {
                Id = auditLog.Id,
                UserId = auditLog.UserId,
                UserName = auditLog.User?.FullName ?? string.Empty,
                UserEmail = auditLog.User?.Email ?? string.Empty,
                Action = auditLog.Action,
                EntityName = auditLog.EntityName,
                EntityId = auditLog.EntityId,
                Details = auditLog.Details,
                IpAddress = auditLog.IpAddress,
                CreatedAt = auditLog.CreatedAt
            };
        }
    }
}