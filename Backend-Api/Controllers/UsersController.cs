using Backend_Api.Data;
using Backend_Api.Helpers;
using Backend_Api.Models;
using Backend_Api.Models.Model_Create;
using Backend_Api.Models.Model_DTO;
using Backend_Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
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
        private readonly IConfiguration _config;

        public UserController(
            LaptopHarbourDbContext context,
            PasswordHasherHelper passwordHasher,
            JwtTokenService jwtService,
            IEmailService emailService,
            IConfiguration config)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _jwtService = jwtService;
            _emailService = emailService;
            _config = config;
        }

        // -----------------------------
        // Signup
        // -----------------------------
        [HttpPost("signup")]
public async Task<IActionResult> Signup([FromBody] Signup dto)
{
    try
    {
        // 1️⃣ Validate model
        if (!ModelState.IsValid)
            return BadRequest(new { message = "Invalid signup data.", errors = ModelState });

        // 2️⃣ Check if username or email already exists
        if (await _context.Users.AnyAsync(u => u.Username == dto.Username || u.Email == dto.Email))
            return BadRequest(new { message = "Username or Email already exists." });

        // 3️⃣ Create user
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
        await _context.SaveChangesAsync(); // Save to get UserId

        // 4️⃣ Create user profile
        _context.UserProfiles.Add(new UserProfile
        {
            UserId = user.UserId,
            CreatedAt = DateTime.UtcNow
        });

        // 5️⃣ Ensure default role exists
        var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "User");
        if (role == null)
        {
            role = new Role
            {
                RoleName = "User",
                CreatedAt = DateTime.UtcNow
            };
            _context.Roles.Add(role);
            await _context.SaveChangesAsync(); // Save to get RoleId
        }

        // 6️⃣ Assign role to user
        _context.UserRoles.Add(new UserRole
        {
            UserId = user.UserId,
            RoleId = role.RoleId
        });

        // 7️⃣ Invalidate old OTPs for this user
        var oldOtps = await _context.UserVerifications
            .Where(v => v.UserId == user.UserId && v.Channel == "email" && !(v.IsUsed ?? false))
            .ToListAsync();

        foreach (var o in oldOtps)
            o.IsUsed = true;

        // 8️⃣ Generate OTP for email verification
        string otp = OTPHelper.GenerateOTP();
        _context.UserVerifications.Add(new UserVerification
        {
            UserId = user.UserId,
            Channel = "email",
            Code = otp,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        // 9️⃣ Send OTP email
        await _emailService.SendEmailAsync(user.Email, "Verify your account", $"Your OTP is: {otp}");

        // 10️⃣ Return success
        return Ok(new
        {
            message = "User registered successfully. Verification OTP sent to email.",
            userId = user.UserId,
            roles = new[] { "User" } // Default role returned to frontend
        });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new
        {
            message = "Error occurred during signup.",
            error = ex.Message
        });
    }
}

        [Authorize]
        [HttpPut("update-profile/{id:int}")]
        public async Task<IActionResult> UpdateProfile(
    int id,
    [FromForm] UpdateFullProfileDto dto
)
        {
            try
            {
                // 1️⃣ Get logged-in user from JWT (FIXED)
                int loggedInUserId = int.Parse(
                    User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0"
                );

                if (loggedInUserId == 0)
                    return Unauthorized(new { message = "Invalid token." });

                // 🔒 User can update ONLY their own profile
                if (loggedInUserId != id)
                    return Forbid("You are not allowed to update another user's profile.");

                // 2️⃣ Load user + profile
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserId == id);

                if (user == null)
                    return NotFound(new { message = "User not found." });

                var profile = await _context.UserProfiles
                    .FirstOrDefaultAsync(p => p.UserId == id);

                if (profile == null)
                    return NotFound(new { message = "User profile not found." });

                // 3️⃣ Update User fields
                if (!string.IsNullOrWhiteSpace(dto.FirstName))
                    user.FirstName = dto.FirstName;

                if (!string.IsNullOrWhiteSpace(dto.LastName))
                    user.LastName = dto.LastName;

                if (!string.IsNullOrWhiteSpace(dto.PhoneNumber))
                    user.PhoneNumber = dto.PhoneNumber;

                if (!string.IsNullOrWhiteSpace(dto.Email) && dto.Email != user.Email)
                {
                    user.Email = dto.Email;
                    user.IsEmailVerified = false;

                    // Invalidate old OTPs
                    var oldOtps = await _context.UserVerifications
                        .Where(v =>
                            v.UserId == id &&
                            v.Channel == "email" &&
                            !(v.IsUsed ?? false)
                        )
                        .ToListAsync();

                    foreach (var o in oldOtps)
                        o.IsUsed = true;

                    // Generate new OTP
                    string otp = OTPHelper.GenerateOTP();
                    _context.UserVerifications.Add(new UserVerification
                    {
                        UserId = id,
                        Channel = "email",
                        Code = otp,
                        ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                        IsUsed = false,
                        CreatedAt = DateTime.UtcNow
                    });

                    await _emailService.SendEmailAsync(
                        user.Email,
                        "Verify your account",
                        $"Your OTP is: {otp}"
                    );
                }

                // 4️⃣ Update Profile fields
                if (!string.IsNullOrWhiteSpace(dto.Bio))
                    profile.Bio = dto.Bio;

                if (dto.ProfileImage != null && dto.ProfileImage.Length > 0)
                {
                    var uploadPath = Path.Combine(
                        _config["StoredFilesPath"] ?? "wwwroot/upload",
                        "UserProfiles"
                    );

                    Directory.CreateDirectory(uploadPath);

                    var ext = Path.GetExtension(dto.ProfileImage.FileName);
                    var imageName = Guid.NewGuid() + ext;
                    var filePath = Path.Combine(uploadPath, imageName);

                    using (var stream = System.IO.File.Create(filePath))
                    {
                        await dto.ProfileImage.CopyToAsync(stream);
                    }

                    // Delete old image
                    if (!string.IsNullOrEmpty(profile.ProfileImage))
                    {
                        var oldFile = Path.Combine(uploadPath, profile.ProfileImage);
                        if (System.IO.File.Exists(oldFile))
                            System.IO.File.Delete(oldFile);
                    }

                    profile.ProfileImage = imageName;
                }

                profile.UpdatedAt = DateTime.UtcNow;

                // 5️⃣ Save changes
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Profile updated successfully.",
                    data = new
                    {
                        user.UserId,
                        user.FirstName,
                        user.LastName,
                        user.Email,
                        user.PhoneNumber,
                        profile.Bio,
                        profile.ProfileImage
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Error occurred while updating profile.",
                    error = ex.Message
                });
            }
        }





        // -----------------------------
        // Login
        // -----------------------------
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginUser dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new { message = "Invalid login data.", errors = ModelState });

                var user = await _context.Users
                    .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u =>
                        u.Username == dto.UsernameOrEmail || u.Email == dto.UsernameOrEmail);

                if (user == null || !_passwordHasher.VerifyPassword(user.PasswordHash, dto.Password))
                    return Unauthorized(new { message = "Invalid username/email or password." });

                if (user.IsActive != true)
                    return BadRequest(new { message = "User account is inactive." });

                // Email not verified → resend OTP
                if (user.IsEmailVerified != true)
                {
                    // Invalidate old OTPs
                    var oldOtps = await _context.UserVerifications
                        .Where(v => v.UserId == user.UserId && v.Channel == "email" && !(v.IsUsed ?? false))
                        .ToListAsync();
                    foreach (var o in oldOtps) o.IsUsed = true;

                    string otp = OTPHelper.GenerateOTP();
                    _context.UserVerifications.Add(new UserVerification
                    {
                        UserId = user.UserId,
                        Channel = "email",
                        Code = otp,
                        ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                        IsUsed = false,
                        CreatedAt = DateTime.UtcNow
                    });

                    await _context.SaveChangesAsync();
                    await _emailService.SendEmailAsync(user.Email, "Verify your account", $"Your OTP is: {otp}");

                    return BadRequest(new
                    {
                        message = "Email not verified. A new OTP has been sent to your email.",
                        userId = user.UserId
                    });
                }

                // Continue login
                var roles = user.UserRoles.Select(ur => ur.Role.RoleName).ToList();
                var jwt = _jwtService.GenerateToken(user.UserId, user.Username, user.Email, roles);

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

                Response.Cookies.Append("refreshToken", refreshToken.Token, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = refreshToken.ExpiresAt
                });

                return Ok(new
                {
                    message = "Login successful.",
                    token = jwt,
                    userId = user.UserId,
                    roles = roles
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error occurred during login.", error = ex.Message });
            }
        }

        // -----------------------------
        // Verify Email
        // -----------------------------
        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailDTO dto)
        {
            try
            {
                var verification = await _context.UserVerifications
                    .Where(v => v.UserId == dto.UserId && v.Channel == "email" && !(v.IsUsed ?? false))
                    .OrderByDescending(v => v.CreatedAt)
                    .FirstOrDefaultAsync();

                if (verification == null)
                    return BadRequest(new { message = "No OTP request found for this user." });

                if (verification.Code != dto.Code)
                    return BadRequest(new { message = "Invalid OTP code." });

                if (verification.ExpiresAt < DateTime.UtcNow)
                    return BadRequest(new { message = "OTP has expired." });

                verification.IsUsed = true;

                var user = await _context.Users.FindAsync(dto.UserId);
                if (user != null) user.IsEmailVerified = true;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Email verified successfully.", userId = user!.UserId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error occurred during email verification.", error = ex.Message });
            }
        }

        // -----------------------------
        // Resend OTP
        // -----------------------------
        [HttpPost("resend-otp")]
        public async Task<IActionResult> ResendOtp([FromBody] ResendOtpDTO dto)
        {
            try
            {
                var user = await _context.Users.FindAsync(dto.UserId);
                if (user == null)
                    return NotFound(new { message = "User not found." });

                if (user.IsEmailVerified == true)
                    return BadRequest(new { message = "Email is already verified." });

                // Invalidate old OTPs
                var oldOtps = await _context.UserVerifications
                    .Where(v => v.UserId == dto.UserId && v.Channel == "email" && !(v.IsUsed ?? false))
                    .ToListAsync();
                foreach (var o in oldOtps) o.IsUsed = true;

                // Generate new OTP
                string otp = OTPHelper.GenerateOTP();
                _context.UserVerifications.Add(new UserVerification
                {
                    UserId = dto.UserId,
                    Channel = "email",
                    Code = otp,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                    IsUsed = false,
                    CreatedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
                await _emailService.SendEmailAsync(user.Email, "Resend OTP", $"Your new OTP is: {otp}");

                return Ok(new { message = "OTP resent successfully.", userId = user.UserId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error occurred while resending OTP.", error = ex.Message });
            }
        }

        // -----------------------------
        // Request Password Reset
        // -----------------------------
        [HttpPost("request-password-reset")]
        public async Task<IActionResult> RequestPasswordReset([FromBody] PasswordResetRequestDTO dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Email))
                    return BadRequest(new { message = "Email is required." });

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
                if (user == null)
                    return BadRequest(new { message = "Email not found." });

                // Invalidate old password reset tokens
                var oldTokens = await _context.PasswordResetTokens
                    .Where(t => t.UserId == user.UserId && !(t.IsUsed ?? false) && t.ExpiresAt > DateTime.UtcNow)
                    .ToListAsync();
                foreach (var t in oldTokens) t.IsUsed = true;

                string token = Guid.NewGuid().ToString();
                _context.PasswordResetTokens.Add(new PasswordResetToken
                {
                    UserId = user.UserId,
                    ResetToken = token,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                    IsUsed = false,
                    CreatedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
                await _emailService.SendEmailAsync(user.Email, "Password Reset", $"Your reset token: {token}");

                return Ok(new { message = "Password reset token sent to your email." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error occurred while requesting password reset.", error = ex.Message });
            }
        }

        // -----------------------------
        // Reset Password
        // -----------------------------
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] PasswordResetDTO dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.NewPassword))
                    return BadRequest(new { message = "New password is required." });

                var reset = await _context.PasswordResetTokens
                    .FirstOrDefaultAsync(r =>
                        r.ResetToken == dto.Token &&
                        !(r.IsUsed ?? false) &&
                        r.ExpiresAt > DateTime.UtcNow);

                if (reset == null)
                    return BadRequest(new { message = "Invalid or expired reset token." });

                var user = await _context.Users.FindAsync(reset.UserId);
                if (user == null)
                    return BadRequest(new { message = "User not found." });

                user.PasswordHash = _passwordHasher.HashPassword(dto.NewPassword);
                reset.IsUsed = true;

                await _context.SaveChangesAsync();
                return Ok(new { message = "Password has been reset successfully.", userId = user.UserId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error occurred while resetting password.", error = ex.Message });
            }
        }

        // -----------------------------
        // Refresh Token
        // -----------------------------
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken()
        {
            try
            {
                if (!Request.Cookies.TryGetValue("refreshToken", out var token))
                    return Unauthorized(new { message = "Refresh token missing." });

                var refreshToken = await _context.RefreshTokens
                    .Include(rt => rt.User)
                        .ThenInclude(u => u.UserRoles)
                            .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(rt =>
                        rt.Token == token &&
                        rt.IsRevoked == false &&
                        rt.ExpiresAt > DateTime.UtcNow);

                if (refreshToken == null)
                    return Unauthorized(new { message = "Invalid or expired refresh token." });

                refreshToken.IsRevoked = true;

                var user = refreshToken.User;
                var roles = user.UserRoles.Select(ur => ur.Role.RoleName).ToList();
                var jwt = _jwtService.GenerateToken(user.UserId, user.Username, user.Email, roles);

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

                Response.Cookies.Append("refreshToken", newRefreshToken.Token, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = newRefreshToken.ExpiresAt
                });

                return Ok(new { message = "Token refreshed successfully.", token = jwt, userId = user.UserId, roles = roles });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error occurred while refreshing token.", error = ex.Message });
            }
        }

        // -----------------------------
        // Logout
        // -----------------------------
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            try
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

                return Ok(new { message = "Logged out successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error occurred during logout.", error = ex.Message });
            }
        }

        // -----------------------------
        // Get User Profile
        // -----------------------------
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetUser(int userId)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.UserProfile)
                    .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(u => u.UserId == userId);

                if (user == null)
                    return NotFound(new { message = "User not found." });

                var dto = new UserDTO
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    PhoneNumber = user.PhoneNumber,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    Roles = user.UserRoles.Select(ur => ur.Role.RoleName).ToList()
                };

                return Ok(new { message = "User profile fetched successfully.", data = dto });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error occurred while fetching user profile.", error = ex.Message });
            }
        }
    }
}
