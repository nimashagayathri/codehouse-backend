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
using RecruitmentPlatform.API.Services;

namespace RecruitmentPlatform.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    [Microsoft.AspNetCore.Cors.EnableCors("ReactPolicy")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;

        public AuthController(ApplicationDbContext context, IConfiguration configuration, IEmailService emailService)
        {
            _context = context;
            _configuration = configuration;
            _emailService = emailService;
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

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email.Trim().ToLower());
            if (user == null)
            {
                return Ok(new { message = "If this email exists, a password reset link has been sent." });
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim("Purpose", "ResetPassword")
                }),
                Expires = DateTime.UtcNow.AddMinutes(15),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"]
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            var resetLink = $"http://localhost:3000/reset-password?token={tokenString}";
            string body = $@"Dear {user.FullName},

You requested to reset your password on CodeHouse API.
Please click the link below to set a new password. This link will expire in 15 minutes.

{resetLink}

If you didn't request this, please ignore this email.

Thanks,
CodeHouse Team";

            try {
                await _emailService.SendEmailAsync(user.Email, "Reset Your CodeHouse Password", body);
            } catch (Exception ex) {
                Console.WriteLine($"[Email Error] {ex.Message}");
            }

            return Ok(new { message = "If this email exists, a password reset link has been sent." });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!);

            try
            {
                tokenHandler.ValidateToken(request.Token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _configuration["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = _configuration["Jwt:Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                
                var purpose = jwtToken.Claims.FirstOrDefault(x => x.Type == "Purpose")?.Value;
                if (purpose != "ResetPassword") return BadRequest(new { message = "Invalid token type." });

                var userIdStr = jwtToken.Claims.First(x => x.Type == "nameid").Value;
                int userId = int.Parse(userIdStr);

                var user = await _context.Users.FindAsync(userId);
                if (user == null) return BadRequest(new { message = "User not found." });

                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Password updated successfully." });
            }
            catch
            {
                return BadRequest(new { message = "Invalid or expired password reset token." });
            }
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