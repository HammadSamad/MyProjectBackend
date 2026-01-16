using Backend_Api.Data;
using Backend_Api.Helpers;
using Backend_Api.Models;
using Backend_Api.Models.Model_Create;
using Backend_Api.Models.Model_DTO;
using Backend_Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Backend_Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly LaptopHarbourDbContext _context;
        private readonly PasswordHasherHelper _passwordHasher;
        private readonly JwtTokenService _jwtService;
        private readonly IEmailService _emailService;

        public UserController(
            LaptopHarbourDbContext context,
            PasswordHasherHelper passwordHasher,
            JwtTokenService jwtService,
            IEmailService emailService)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _jwtService = jwtService;
            _emailService = emailService;
        }

        // -----------------------------
        // Signup
        // -----------------------------
        [HttpPost("signup")]
        public async Task<IActionResult> Signup([FromBody] Signup dto)
        {
            if (await _context.Users.AnyAsync(u => u.Username == dto.Username || u.Email == dto.Email))
                return BadRequest("Username or Email already exists.");

            var user = new User
            {
                Username = dto.Username,
                Email = dto.Email,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                PhoneNumber = dto.PhoneNumber,
                PasswordHash = _passwordHasher.HashPassword(dto.Password),
                IsActive = true,
                IsEmailVerified = false,
                IsPhoneVerified = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Generate OTP using OTPHelper
            string otp = OTPHelper.GenerateOTP();
            var verification = new UserVerification
            {
                UserId = user.UserId,
                Channel = "email",
                Code = otp,
                ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                IsUsed = false,
                CreatedAt = DateTime.UtcNow
            };
            _context.UserVerifications.Add(verification);
            await _context.SaveChangesAsync();

            // Send OTP via email
            await _emailService.SendEmailAsync(user.Email, "Verify your account", $"Your verification code is: {otp}");

            return Ok(new { message = "User registered successfully. Verification OTP sent." });
        }

        // -----------------------------
        // Login
        // -----------------------------
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginUser dto)
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Username == dto.UsernameOrEmail || u.Email == dto.UsernameOrEmail);

            if (user == null || !_passwordHasher.VerifyPassword(user.PasswordHash, dto.Password))
                return Unauthorized("Invalid credentials.");

            if (!user.IsActive)
                return BadRequest("User is inactive.");

            var roles = user.UserRoles.Select(ur => ur.Role.RoleName).ToList();
            var token = _jwtService.GenerateToken(user.UserId, user.Username, user.Email, roles);

            return Ok(new { token });
        }

        // -----------------------------
        // Verify Email OTP
        // -----------------------------
        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail(int userId, string code)
        {
            var verification = await _context.UserVerifications
                .Where(v => v.UserId == userId && v.Channel == "email" && v.IsUsed == false)
                .OrderByDescending(v => v.CreatedAt)
                .FirstOrDefaultAsync();

            if (verification == null || verification.Code != code || verification.ExpiresAt < DateTime.UtcNow)
                return BadRequest("Invalid or expired OTP.");

            verification.IsUsed = true;

            var user = await _context.Users.FindAsync(userId);
            if (user != null) user.IsEmailVerified = true;

            await _context.SaveChangesAsync();
            return Ok("Email verified successfully.");
        }

        // -----------------------------
        // Request Password Reset
        // -----------------------------
        [HttpPost("request-password-reset")]
        public async Task<IActionResult> RequestPasswordReset(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) return BadRequest("Email not found.");

            string token = Guid.NewGuid().ToString();
            var reset = new PasswordResetToken
            {
                UserId = user.UserId,
                ResetToken = token,
                ExpiresAt = DateTime.UtcNow.AddMinutes(30),
                IsUsed = false,
                CreatedAt = DateTime.UtcNow
            };
            _context.PasswordResetTokens.Add(reset);
            await _context.SaveChangesAsync();

            await _emailService.SendEmailAsync(user.Email, "Password Reset", $"Your reset token: {token}");
            return Ok("Password reset token sent to your email.");
        }

        // -----------------------------
        // Reset Password
        // -----------------------------
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(string token, string newPassword)
        {
            var reset = await _context.PasswordResetTokens
                .FirstOrDefaultAsync(r => r.ResetToken == token && r.IsUsed == false && r.ExpiresAt > DateTime.UtcNow);

            if (reset == null) return BadRequest("Invalid or expired token.");

            var user = await _context.Users.FindAsync(reset.UserId);
            if (user == null) return BadRequest("User not found.");

            user.PasswordHash = _passwordHasher.HashPassword(newPassword);
            reset.IsUsed = true;

            await _context.SaveChangesAsync();
            return Ok("Password has been reset successfully.");
        }

        // -----------------------------
        // Get User Profile
        // -----------------------------
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetUser(int userId)
        {
            var user = await _context.Users
                .Include(u => u.UserProfile)
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null) return NotFound();

            var dto = new UserDTO
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt
            };

            return Ok(dto);
        }
    }
}
