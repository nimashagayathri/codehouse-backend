using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RecruitmentPlatform.API.Data;
using RecruitmentPlatform.API.DTOs.Auth;
using RecruitmentPlatform.API.Helpers;
using RecruitmentPlatform.API.Models;

namespace RecruitmentPlatform.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    [Microsoft.AspNetCore.Cors.EnableCors("ReactPolicy")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            string? normalizedRole = UserRoles.Normalize(request.Role);

            if (normalizedRole == null)
            {
                return BadRequest(new
                {
                    message = "Invalid role. Allowed roles are Candidate, Recruiter, HiringManager, Admin."
                });
            }

            string email = request.Email.Trim().ToLower();

            bool emailExists = await _context.Users
                .AnyAsync(u => u.Email == email);

            if (emailExists)
            {
                return BadRequest(new
                {
                    message = "Email already registered."
                });
            }

            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            User user = new User
            {
                FullName = request.FullName.Trim(),
                Email = email,
                PasswordHash = passwordHash,
                Role = normalizedRole,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "User registered successfully.",
                userId = user.Id,
                role = user.Role
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            Console.WriteLine("Login: Starting login request...");
            string email = request.Email.Trim().ToLower();

            Console.WriteLine("Login: Fetching user...");
            User? user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);
            Console.WriteLine("Login: User fetched. user null? " + (user == null));

            if (user == null)
            {
                return Unauthorized(new
                {
                    message = "Invalid email or password."
                });
            }

            Console.WriteLine("Login: Verifying password...");
            bool passwordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
            Console.WriteLine("Login: Password valid? " + passwordValid);

            if (!passwordValid)
            {
                return Unauthorized(new
                {
                    message = "Invalid email or password."
                });
            }

            Console.WriteLine("Login: Checking active status...");

            if (!user.IsActive)
            {
                return StatusCode(403, new
                {
                    message = "User account is inactive."
                });
            }

            Console.WriteLine("Login: Generating JWT token...");
            string token = GenerateJwtToken(user);
            Console.WriteLine("Login: Token generated successfully.");

            AuthResponse response = new AuthResponse
            {
                Token = token,
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role
            };

            return Ok(response);
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            string? userIdText = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdText, out int userId))
            {
                return Unauthorized(new
                {
                    message = "Invalid token."
                });
            }

            User? user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return NotFound(new
                {
                    message = "User not found."
                });
            }

            return Ok(new
            {
                user.Id,
                user.FullName,
                user.Email,
                user.Role,
                user.IsActive,
                user.CreatedAt
            });
        }

        private string GenerateJwtToken(User user)
        {
            string? jwtKey = _configuration["Jwt:Key"];
            string? jwtIssuer = _configuration["Jwt:Issuer"];
            string? jwtAudience = _configuration["Jwt:Audience"];
            string? expiresInMinutesText = _configuration["Jwt:ExpiresInMinutes"];

            if (string.IsNullOrWhiteSpace(jwtKey))
            {
                throw new InvalidOperationException("JWT Key is missing.");
            }

            double expiresInMinutes = 120;

            if (!string.IsNullOrWhiteSpace(expiresInMinutesText))
            {
                double.TryParse(expiresInMinutesText, out expiresInMinutes);
            }

            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role),

                // Extra simple claims for frontend usage
                new Claim("userId", user.Id.ToString()),
                new Claim("fullName", user.FullName),
                new Claim("email", user.Email),
                new Claim("role", user.Role)
            };

            SymmetricSecurityKey securityKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey));

            SigningCredentials credentials = new SigningCredentials(
                securityKey,
                SecurityAlgorithms.HmacSha256);

            JwtSecurityToken token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiresInMinutes),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}