using Backend_Api.Data;
using Backend_Api.Models;
using Backend_Api.Models.Model_Create;
using Backend_Api.Models.Model_DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Backend_Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserProfileController : ControllerBase
    {
        private readonly LaptopHarbourDbContext _context;
        private readonly IWebHostEnvironment _env;

        public UserProfileController(
            LaptopHarbourDbContext context,
            IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // -------------------------------------------------------
        // Helper: Generate Full Image URL
        // -------------------------------------------------------
        private string GetImageUrl(string? fileName)
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            if (string.IsNullOrEmpty(fileName))
                return $"{baseUrl}/assets/user/default-user.png";

            return $"{baseUrl}/upload/UserProfiles/{fileName}";
        }

        // -------------------------------------------------------
        // POST: api/UserProfile → Create profile
        // -------------------------------------------------------
        [HttpPost]
        public async Task<IActionResult> CreateProfile([FromForm] CreateUserProfile model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                if (model == null || model.UserId <= 0)
                    return BadRequest(new { error = "Invalid profile data." });

                if (await _context.UserProfiles.AnyAsync(p => p.UserId == model.UserId))
                    return BadRequest(new { error = "Profile already exists for this user." });

                string? imageName = null;
                var uploadPath = Path.Combine(_env.WebRootPath, "upload", "UserProfiles");
                Directory.CreateDirectory(uploadPath);

                // -------------------------
                // Image Upload
                // -------------------------
                if (model.ProfileImage != null && model.ProfileImage.Length > 0)
                {
                    // Validate extension
                    var allowedExt = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                    var ext = Path.GetExtension(model.ProfileImage.FileName).ToLower();

                    if (!allowedExt.Contains(ext))
                        return BadRequest(new { error = "Invalid image format. Allowed: jpg, jpeg, png, webp." });

                    // Validate size (2MB)
                    if (model.ProfileImage.Length > 2 * 1024 * 1024)
                        return BadRequest(new { error = "Image must be under 2MB." });

                    imageName = Guid.NewGuid() + ext;
                    var filePath = Path.Combine(uploadPath, imageName);

                    using var stream = new FileStream(filePath, FileMode.Create);
                    await model.ProfileImage.CopyToAsync(stream);
                }

                var profile = new UserProfile
                {
                    UserId = model.UserId,
                    ProfileImage = imageName,
                    Bio = model.Bio,
                    CreatedAt = DateTime.UtcNow
                };

                _context.UserProfiles.Add(profile);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "User profile created successfully.",
                    profileId = profile.ProfileId,
                    imageUrl = GetImageUrl(profile.ProfileImage)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Failed to create user profile.",
                    details = ex.Message
                });
            }
        }

        // -------------------------------------------------------
        // PUT: api/UserProfile/{id}
        // -------------------------------------------------------
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateProfile(int id, [FromForm] CreateUserProfile model)
        {
            try
            {
                var profile = await _context.UserProfiles.FindAsync(id);
                if (profile == null)
                    return NotFound(new { error = "Profile not found." });

                var uploadPath = Path.Combine(_env.WebRootPath, "upload", "UserProfiles");
                Directory.CreateDirectory(uploadPath);

                profile.Bio = model.Bio ?? profile.Bio;

                if (model.ProfileImage != null && model.ProfileImage.Length > 0)
                {
                    var allowedExt = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                    var ext = Path.GetExtension(model.ProfileImage.FileName).ToLower();

                    if (!allowedExt.Contains(ext))
                        return BadRequest(new { error = "Invalid image format." });

                    if (model.ProfileImage.Length > 2 * 1024 * 1024)
                        return BadRequest(new { error = "Image must be under 2MB." });

                    var imageName = Guid.NewGuid() + ext;
                    var filePath = Path.Combine(uploadPath, imageName);

                    using var stream = new FileStream(filePath, FileMode.Create);
                    await model.ProfileImage.CopyToAsync(stream);

                    // Safe delete old image
                    if (!string.IsNullOrEmpty(profile.ProfileImage))
                    {
                        var oldFile = Path.Combine(uploadPath, profile.ProfileImage);
                        try
                        {
                            if (System.IO.File.Exists(oldFile))
                                System.IO.File.Delete(oldFile);
                        }
                        catch { }
                    }

                    profile.ProfileImage = imageName;
                }

                profile.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "User profile updated successfully.",
                    imageUrl = GetImageUrl(profile.ProfileImage)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Failed to update user profile.",
                    details = ex.Message
                });
            }
        }

        // -------------------------------------------------------
        // GET: api/UserProfile/user/{userId}
        // -------------------------------------------------------
        [HttpGet("user/{userId:int}")]
        public async Task<IActionResult> GetByUserId(int userId)
        {
            try
            {
                var profile = await _context.UserProfiles
                    .Where(p => p.UserId == userId)
                    .FirstOrDefaultAsync();

                if (profile == null)
                    return NotFound(new { error = "Profile not found." });

                var dto = new UserProfileDTO
                {
                    ProfileId = profile.ProfileId,
                    ProfileImage = GetImageUrl(profile.ProfileImage),
                    Bio = profile.Bio,
                    CreatedAt = profile.CreatedAt
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Failed to fetch user profile.",
                    details = ex.Message
                });
            }
        }

        // -------------------------------------------------------
        // DELETE: api/UserProfile/{id}
        // -------------------------------------------------------
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteProfile(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var profile = await _context.UserProfiles.FindAsync(id);
                if (profile == null)
                    return NotFound(new { error = "Profile not found." });

                var uploadPath = Path.Combine(_env.WebRootPath, "upload", "UserProfiles");

                if (!string.IsNullOrEmpty(profile.ProfileImage))
                {
                    var filePath = Path.Combine(uploadPath, profile.ProfileImage);
                    try
                    {
                        if (System.IO.File.Exists(filePath))
                            System.IO.File.Delete(filePath);
                    }
                    catch { }
                }

                _context.UserProfiles.Remove(profile);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return Ok(new { message = "User profile deleted successfully." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                return StatusCode(500, new
                {
                    error = "Failed to delete user profile.",
                    details = ex.Message
                });
            }
        }
    }
}