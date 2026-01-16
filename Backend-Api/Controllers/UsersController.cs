using Backend_Api.Data;
using Backend_Api.Helpers;
using Backend_Api.Models;
using Backend_Api.Models.Model_Create;
using Backend_Api.Models.Model_DTO;
using Backend_Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

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

            // Create default UserProfile
            var profile = new UserProfile
            {
                UserId = user.UserId,
                CreatedAt = DateTime.UtcNow
            };
            _context.UserProfiles.Add(profile);

            // Generate OTP
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

            // Send OTP
            await _emailService.SendEmailAsync(user.Email, "Verify your account", $"Your OTP is: {otp}");

            return Ok(new { message = "User registered successfully. Verification OTP sent." });
        }

        // -----------------------------
        // Login
        // -----------------------------
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginUser dto)
        {
            var user = await _context.Users
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Username == dto.UsernameOrEmail || u.Email == dto.UsernameOrEmail);

            if (user == null)
                return Unauthorized("Invalid credentials.");

            if (!_passwordHasher.VerifyPassword(user.PasswordHash, dto.Password))
                return Unauthorized("Invalid credentials.");

            if (user.IsActive == false)
                return BadRequest("User is inactive.");

            if (user.IsEmailVerified == false)
                return BadRequest("Email not verified. Please verify your email first.");

            // Generate JWT
            var roles = user.UserRoles.Select(ur => ur.Role.RoleName).ToList();
            var jwt = _jwtService.GenerateToken(user.UserId, user.Username, user.Email, roles);

            // Generate RefreshToken
            var refreshToken = new RefreshToken
            {
                UserId = user.UserId,
                Token = Guid.NewGuid().ToString(),
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow
            };
            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            // Set refresh token cookie
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true, // set false if testing without HTTPS
                SameSite = SameSiteMode.Strict,
                Expires = refreshToken.ExpiresAt
            };
            Response.Cookies.Append("refreshToken", refreshToken.Token, cookieOptions);

            return Ok(new { token = jwt });
        }

        // -----------------------------
        // Refresh JWT
        // -----------------------------
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken()
        {
            if (!Request.Cookies.TryGetValue("refreshToken", out var token))
                return Unauthorized("Refresh token missing.");

            var refreshToken = await _context.RefreshTokens
                .Include(rt => rt.User)
                .ThenInclude(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(rt => rt.Token == token && !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow);

            if (refreshToken == null)
                return Unauthorized("Invalid or expired refresh token.");

            // Revoke old token
            refreshToken.IsRevoked = true;

            // Generate new JWT
            var user = refreshToken.User;
            var roles = user.UserRoles.Select(ur => ur.Role.RoleName).ToList();
            var jwt = _jwtService.GenerateToken(user.UserId, user.Username, user.Email, roles);

            // Generate new refresh token
            var newRefreshToken = new RefreshToken
            {
                UserId = user.UserId,
                Token = Guid.NewGuid().ToString(),
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow
            };
            _context.RefreshTokens.Add(newRefreshToken);
            await _context.SaveChangesAsync();

            // Set new cookie
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = newRefreshToken.ExpiresAt
            };
            Response.Cookies.Append("refreshToken", newRefreshToken.Token, cookieOptions);

            return Ok(new { token = jwt });
        }

        // -----------------------------
        // Logout
        // -----------------------------
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            if (Request.Cookies.TryGetValue("refreshToken", out var token))
            {
                var refreshToken = await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == token);
                if (refreshToken != null)
                {
                    refreshToken.IsRevoked = true;
                    await _context.SaveChangesAsync();
                }

                Response.Cookies.Delete("refreshToken");
            }

            return Ok("Logged out successfully.");
        }

        // -----------------------------
        // Verify Email OTP
        // -----------------------------
        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailDTO dto)
        {
            var verification = await _context.UserVerifications
                .Where(v => v.UserId == dto.UserId && v.Channel == "email" && v.IsUsed != true)
                .OrderByDescending(v => v.CreatedAt)
                .FirstOrDefaultAsync();

            if (verification == null || verification.Code != dto.Code || verification.ExpiresAt < DateTime.UtcNow)
                return BadRequest("Invalid or expired OTP.");

            verification.IsUsed = true;

            var user = await _context.Users.FindAsync(dto.UserId);
            if (user != null) user.IsEmailVerified = true;

            await _context.SaveChangesAsync();
            return Ok("Email verified successfully.");
        }

        // -----------------------------
        // Request Password Reset
        // -----------------------------
        [HttpPost("request-password-reset")]
        public async Task<IActionResult> RequestPasswordReset([FromBody] PasswordResetRequestDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Email))
                return BadRequest("Email is required.");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
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
        public async Task<IActionResult> ResetPassword([FromBody] PasswordResetDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.NewPassword))
                return BadRequest("New password is required.");

            var reset = await _context.PasswordResetTokens
                .FirstOrDefaultAsync(r => r.ResetToken == dto.Token && r.IsUsed != true && r.ExpiresAt > DateTime.UtcNow);

            if (reset == null) return BadRequest("Invalid or expired token.");

            var user = await _context.Users.FindAsync(reset.UserId);
            if (user == null) return BadRequest("User not found.");

            user.PasswordHash = _passwordHasher.HashPassword(dto.NewPassword);
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
