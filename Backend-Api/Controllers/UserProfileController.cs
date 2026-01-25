using Backend_Api.Data;
using Backend_Api.Models;
using Backend_Api.Models.Model_Create;
using Backend_Api.Models.Model_DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Backend_Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserProfileController : ControllerBase
    {
        private readonly LaptopHarbourDbContext _context;
        private readonly IConfiguration _config;

        public UserProfileController(LaptopHarbourDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // -------------------------------------------------------
        // POST: api/UserProfile → Create profile with image
        // -------------------------------------------------------
        [HttpPost]
        public async Task<IActionResult> CreateProfile([FromForm] CreateUserProfile model)
        {
            try
            {
                if (model == null || model.UserId <= 0)
                    return BadRequest(new { error = "Invalid profile data." });

                if (await _context.UserProfiles.AnyAsync(p => p.UserId == model.UserId))
                    return BadRequest(new { error = "Profile already exists for this user." });

                string? imageName = null;

                // Upload Image
                if (model.ProfileImage != null && model.ProfileImage.Length > 0)
                {
                    var uploadPath = Path.Combine(_config["StoredFilesPath"] ?? "wwwroot/upload", "UserProfiles");
                    Directory.CreateDirectory(uploadPath);

                    var ext = Path.GetExtension(model.ProfileImage.FileName);
                    imageName = Guid.NewGuid() + ext;
                    var filePath = Path.Combine(uploadPath, imageName);

                    using var stream = System.IO.File.Create(filePath);
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

                return Ok(new { message = "User profile created successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to create user profile.", details = ex.Message });
            }
        }

        // -------------------------------------------------------
        // PUT: api/UserProfile/{id} → Update profile + image
        // -------------------------------------------------------
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateProfile(int id, [FromForm] CreateUserProfile model)
        {
            try
            {
                var profile = await _context.UserProfiles.FindAsync(id);
                if (profile == null)
                    return NotFound(new { error = "Profile not found." });

                // Bio update
                profile.Bio = model.Bio ?? profile.Bio;

                // Image update
                if (model.ProfileImage != null && model.ProfileImage.Length > 0)
                {
                    var uploadPath = Path.Combine(_config["StoredFilesPath"] ?? "wwwroot/upload", "UserProfiles");
                    Directory.CreateDirectory(uploadPath);

                    var ext = Path.GetExtension(model.ProfileImage.FileName);
                    var imageName = Guid.NewGuid() + ext;
                    var filePath = Path.Combine(uploadPath, imageName);

                    using var stream = System.IO.File.Create(filePath);
                    await model.ProfileImage.CopyToAsync(stream);

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

                await _context.SaveChangesAsync();
                return Ok(new { message = "User profile updated successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to update user profile.", details = ex.Message });
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
                    .Select(p => new UserProfileDTO
                    {
                        ProfileId = p.ProfileId,
                        ProfileImage = p.ProfileImage,
                        Bio = p.Bio,
                        CreatedAt = p.CreatedAt
                    })
                    .FirstOrDefaultAsync();

                if (profile == null)
                    return NotFound(new { error = "Profile not found." });

                return Ok(profile);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to fetch user profile.", details = ex.Message });
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

                // Delete image file
                if (!string.IsNullOrEmpty(profile.ProfileImage))
                {
                    var filePath = Path.Combine(
                        _config["StoredFilesPath"] ?? "wwwroot/upload",
                        "UserProfiles",
                        profile.ProfileImage);

                    if (System.IO.File.Exists(filePath))
                        System.IO.File.Delete(filePath);
                }

                _context.UserProfiles.Remove(profile);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return Ok(new { message = "User profile deleted successfully." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { error = "Failed to delete user profile.", details = ex.Message });
            }
        }
    }
}
