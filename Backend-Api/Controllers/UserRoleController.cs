using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backend_Api.Data;
using Backend_Api.Models;
using Backend_Api.Models.Model_Create;
using Backend_Api.Models.Model_DTO;

namespace Backend_Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserRoleController : ControllerBase
    {
        private readonly LaptopHarbourDbContext _context;

        public UserRoleController(LaptopHarbourDbContext context)
        {
            _context = context;
        }

        // ---------------------------------------
        // 1. Assign Role to User
        // POST: api/userrole
        // ---------------------------------------
        [HttpPost]
        public async Task<IActionResult> AssignRoleToUser([FromBody] CreateUserRole dto)
        {
            // Check User exists
            if (!await _context.Users.AnyAsync(u => u.UserId == dto.UserId))
                return BadRequest("User does not exist.");

            // Check Role exists
            if (!await _context.Roles.AnyAsync(r => r.RoleId == dto.RoleId))
                return BadRequest("Role does not exist.");

            // Prevent duplicate role assignment
            var exists = await _context.UserRoles.AnyAsync(ur =>
                ur.UserId == dto.UserId && ur.RoleId == dto.RoleId);

            if (exists)
                return BadRequest("This role is already assigned to the user.");

            var userRole = new UserRole
            {
                UserId = dto.UserId,
                RoleId = dto.RoleId,
                CreatedAt = DateTime.UtcNow
            };

            _context.UserRoles.Add(userRole);
            await _context.SaveChangesAsync();

            return Ok(new UserRoleDTO
            {
                UserId = userRole.UserId,
                RoleId = userRole.RoleId,
                CreatedAt = userRole.CreatedAt
            });
        }

        // ---------------------------------------
        // 2. Get All User Roles
        // GET: api/userrole
        // ---------------------------------------
        [HttpGet]
        public async Task<IActionResult> GetAllUserRoles()
        {
            var data = await _context.UserRoles
                .Include(ur => ur.Role)
                .Include(ur => ur.User)
                .Select(ur => new UserRoleDTO
                {
                    UserId = ur.UserId,
                    RoleId = ur.RoleId,
                    RoleName = ur.Role.RoleName,
                    CreatedAt = ur.CreatedAt
                })
                .ToListAsync();

            return Ok(data);
        }

        // ---------------------------------------
        // 3. Get Roles by User Id
        // GET: api/userrole/user/{userId}
        // ---------------------------------------
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetRolesByUser(int userId)
        {
            var data = await _context.UserRoles
                .Include(ur => ur.Role)
                .Where(ur => ur.UserId == userId)
                .Select(ur => new UserRoleDTO
                {
                    UserId = ur.UserId,
                    RoleId = ur.RoleId,
                    RoleName = ur.Role.RoleName,
                    CreatedAt = ur.CreatedAt
                })
                .ToListAsync();

            if (!data.Any())
                return NotFound("No roles found for this user.");

            return Ok(data);
        }

        // ---------------------------------------
        // 4. Get Users by Role Id
        // GET: api/userrole/role/{roleId}
        // ---------------------------------------
        [HttpGet("role/{roleId}")]
        public async Task<IActionResult> GetUsersByRole(int roleId)
        {
            var data = await _context.UserRoles
                .Include(ur => ur.User)
                .Where(ur => ur.RoleId == roleId)
                .Select(ur => new
                {
                    ur.UserId,
                    ur.User.Username,
                    ur.User.Email,
                    ur.CreatedAt
                })
                .ToListAsync();

            if (!data.Any())
                return NotFound("No users found for this role.");

            return Ok(data);
        }

        // ---------------------------------------
        // 5. Remove Role from User
        // DELETE: api/userrole
        // ---------------------------------------
        [HttpDelete]
        public async Task<IActionResult> RemoveRoleFromUser([FromBody] CreateUserRole dto)
        {
            var userRole = await _context.UserRoles
                .FirstOrDefaultAsync(ur =>
                    ur.UserId == dto.UserId &&
                    ur.RoleId == dto.RoleId);

            if (userRole == null)
                return NotFound("User-Role mapping not found.");

            _context.UserRoles.Remove(userRole);
            await _context.SaveChangesAsync();

            return Ok("Role removed from user successfully.");
        }

        // ---------------------------------------
        // 6. Remove All Roles from User
        // DELETE: api/userrole/user/{userId}
        // ---------------------------------------
        [HttpDelete("user/{userId}")]
        public async Task<IActionResult> RemoveAllRolesFromUser(int userId)
        {
            var mappings = await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .ToListAsync();

            if (!mappings.Any())
                return NotFound("No roles found for this user.");

            _context.UserRoles.RemoveRange(mappings);
            await _context.SaveChangesAsync();

            return Ok("All roles removed from user.");
        }
    }
}
