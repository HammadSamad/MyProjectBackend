using Backend_Api.Data;
using Backend_Api.Models;
using Backend_Api.Models.Model_Create;
using Backend_Api.Models.Model_DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
            if (model == null || model.UserId <= 0)
                return BadRequest("Invalid profile data.");

            if (await _context.UserProfiles.AnyAsync(p => p.UserId == model.UserId))
                return BadRequest("Profile already exists for this user.");

            string? imageName = null;

            // Upload Image (same style as Employee)
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

        // -------------------------------------------------------
        // PUT: api/UserProfile/{id} → Update profile + image
        // -------------------------------------------------------
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateProfile(int id, [FromForm] CreateUserProfile model)
        {
            var profile = await _context.UserProfiles.FindAsync(id);
            if (profile == null)
                return NotFound("Profile not found.");

            // Bio update
            profile.Bio = model.Bio ?? profile.Bio;

            // Image update (same logic as Employee)
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
                    var oldFile = Path.Combine(
                        _config["StoredFilesPath"] ?? "wwwroot/upload",
                        "UserProfiles",
                        profile.ProfileImage);

                    if (System.IO.File.Exists(oldFile))
                        System.IO.File.Delete(oldFile);
                }

                profile.ProfileImage = imageName;
            }

            profile.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { message = "User profile updated successfully." });
        }

        // -------------------------------------------------------
        // GET: api/UserProfile/user/{userId}
        // -------------------------------------------------------
        [HttpGet("user/{userId:int}")]
        public async Task<IActionResult> GetByUserId(int userId)
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
                return NotFound("Profile not found.");

            return Ok(profile);
        }

        // -------------------------------------------------------
        // DELETE: api/UserProfile/{id}
        // -------------------------------------------------------
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteProfile(int id)
        {
            var profile = await _context.UserProfiles.FindAsync(id);
            if (profile == null)
                return NotFound("Profile not found.");

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

            return Ok(new { message = "User profile deleted successfully." });
        }
    }
}
