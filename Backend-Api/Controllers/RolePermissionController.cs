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
    public class RolePermissionController : ControllerBase
    {
        private readonly LaptopHarbourDbContext _context;

        public RolePermissionController(LaptopHarbourDbContext context)
        {
            _context = context;
        }

        // ---------------------------------------
        // 1. Assign Permission to Role
        // POST: api/rolepermission
        // ---------------------------------------
        [HttpPost]
        public async Task<IActionResult> AssignPermissionToRole(CreateRolePermission dto)
        {
            // Check role exists
            if (!await _context.Roles.AnyAsync(r => r.RoleId == dto.RoleId))
                return BadRequest("Role does not exist.");

            // Check permission exists
            if (!await _context.Permissions.AnyAsync(p => p.PermissionId == dto.PermissionId))
                return BadRequest("Permission does not exist.");

            // Prevent duplicate
            var exists = await _context.RolePermissions.AnyAsync(rp =>
                rp.RoleId == dto.RoleId && rp.PermissionId == dto.PermissionId);

            if (exists)
                return BadRequest("This permission is already assigned to this role.");

            var rolePermission = new RolePermission
            {
                RoleId = dto.RoleId,
                PermissionId = dto.PermissionId,
                CreatedAt = DateTime.UtcNow
            };

            _context.RolePermissions.Add(rolePermission);
            await _context.SaveChangesAsync();

            return Ok(new RolePermissionDTO
            {
                RoleId = rolePermission.RoleId,
                PermissionId = rolePermission.PermissionId,
                CreatedAt = rolePermission.CreatedAt
            });
        }

        // ---------------------------------------
        // 2. Get All Role-Permission Mappings
        // GET: api/rolepermission
        // ---------------------------------------
        [HttpGet]
        public async Task<IActionResult> GetAllRolePermissions()
        {
            var data = await _context.RolePermissions
                .Include(rp => rp.Role)
                .Include(rp => rp.Permission)
                .Select(rp => new RolePermissionDTO
                {
                    RoleId = rp.RoleId,
                    PermissionId = rp.PermissionId,
                    PermissionName = rp.Permission.PermissionName,
                    CreatedAt = rp.CreatedAt
                })
                .ToListAsync();

            return Ok(data);
        }

        // ---------------------------------------
        // 3. Get Permissions By Role Id
        // GET: api/rolepermission/role/{roleId}
        // ---------------------------------------
        [HttpGet("role/{roleId}")]
        public async Task<IActionResult> GetPermissionsByRole(int roleId)
        {
            var data = await _context.RolePermissions
                .Include(rp => rp.Permission)
                .Where(rp => rp.RoleId == roleId)
                .Select(rp => new RolePermissionDTO
                {
                    RoleId = rp.RoleId,
                    PermissionId = rp.PermissionId,
                    PermissionName = rp.Permission.PermissionName,
                    CreatedAt = rp.CreatedAt
                })
                .ToListAsync();

            if (!data.Any())
                return NotFound("No permissions found for this role.");

            return Ok(data);
        }

        // ---------------------------------------
        // 4. Get Roles By Permission Id
        // GET: api/rolepermission/permission/{permissionId}
        // ---------------------------------------
        [HttpGet("permission/{permissionId}")]
        public async Task<IActionResult> GetRolesByPermission(int permissionId)
        {
            var data = await _context.RolePermissions
                .Include(rp => rp.Role)
                .Where(rp => rp.PermissionId == permissionId)
                .Select(rp => new
                {
                    rp.RoleId,
                    rp.Role.RoleName,
                    rp.CreatedAt
                })
                .ToListAsync();

            if (!data.Any())
                return NotFound("No roles found for this permission.");

            return Ok(data);
        }

        // ---------------------------------------
        // 5. Delete (Unassign Permission from Role)
        // DELETE: api/rolepermission
        // ---------------------------------------
        [HttpDelete]
        public async Task<IActionResult> RemovePermissionFromRole(CreateRolePermission dto)
        {
            var rolePermission = await _context.RolePermissions
                .FirstOrDefaultAsync(rp =>
                    rp.RoleId == dto.RoleId &&
                    rp.PermissionId == dto.PermissionId);

            if (rolePermission == null)
                return NotFound("Mapping not found.");

            _context.RolePermissions.Remove(rolePermission);
            await _context.SaveChangesAsync();

            return Ok("Permission removed from role successfully.");
        }

        // ---------------------------------------
        // 6. Delete All Permissions of a Role
        // DELETE: api/rolepermission/role/{roleId}
        // ---------------------------------------
        [HttpDelete("role/{roleId}")]
        public async Task<IActionResult> RemoveAllPermissionsFromRole(int roleId)
        {
            var mappings = await _context.RolePermissions
                .Where(rp => rp.RoleId == roleId)
                .ToListAsync();

            if (!mappings.Any())
                return NotFound("No permissions found for this role.");

            _context.RolePermissions.RemoveRange(mappings);
            await _context.SaveChangesAsync();

            return Ok("All permissions removed from role.");
        }
    }
}
