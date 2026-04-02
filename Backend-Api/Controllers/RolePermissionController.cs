using Backend_Api.Data;
using Backend_Api.Models;
using Backend_Api.Models.Model_Create;
using Backend_Api.Models.Model_DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

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

        // -----------------------------
        // 1. Assign Permission to Role (Admin only)
        // POST: api/rolepermission
        // -----------------------------
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AssignPermissionToRole(CreateRolePermission dto)
        {
            try
            {
                if (!await _context.Roles.AnyAsync(r => r.RoleId == dto.RoleId))
                    return BadRequest(new { error = "Role does not exist." });

                if (!await _context.Permissions.AnyAsync(p => p.PermissionId == dto.PermissionId))
                    return BadRequest(new { error = "Permission does not exist." });

                var exists = await _context.RolePermissions
                    .AnyAsync(rp => rp.RoleId == dto.RoleId && rp.PermissionId == dto.PermissionId);

                if (exists)
                    return BadRequest(new { error = "This permission is already assigned to this role." });

                var rolePermission = new RolePermission
                {
                    RoleId = dto.RoleId,
                    PermissionId = dto.PermissionId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.RolePermissions.Add(rolePermission);
                await _context.SaveChangesAsync();

                var rpDto = await _context.RolePermissions
                    .Include(rp => rp.Role)
                    .Include(rp => rp.Permission)
                    .Where(rp => rp.RoleId == dto.RoleId && rp.PermissionId == dto.PermissionId)
                    .Select(rp => new RolePermissionDTO
                    {
                        RoleId = rp.RoleId,
                        RoleName = rp.Role.RoleName,
                        PermissionId = rp.PermissionId,
                        PermissionName = rp.Permission.PermissionName,
                        CreatedAt = rp.CreatedAt
                    })
                    .FirstOrDefaultAsync();

                return Ok(rpDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to assign permission to role.", details = ex.Message });
            }
        }

        // -----------------------------
        // 2. Get All Role-Permission Mappings (Consistent DTO)
        // GET: api/rolepermission
        // -----------------------------
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllRolePermissions()
        {
            try
            {
                var data = await _context.RolePermissions
                    .Include(rp => rp.Role)
                    .Include(rp => rp.Permission)
                    .Select(rp => new RolePermissionDTO
                    {
                        RoleId = rp.RoleId,
                        RoleName = rp.Role.RoleName,
                        PermissionId = rp.PermissionId,
                        PermissionName = rp.Permission.PermissionName,
                        CreatedAt = rp.CreatedAt
                    })
                    .ToListAsync();

                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to fetch role-permission mappings.", details = ex.Message });
            }
        }

        // -----------------------------
        // 3. Get Permissions By Role Id (Consistent DTO)
        // GET: api/rolepermission/role/{roleId}
        // -----------------------------
        [HttpGet("role/{roleId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPermissionsByRole(int roleId)
        {
            try
            {
                var data = await _context.RolePermissions
                    .Include(rp => rp.Role)
                    .Include(rp => rp.Permission)
                    .Where(rp => rp.RoleId == roleId)
                    .Select(rp => new RolePermissionDTO
                    {
                        RoleId = rp.RoleId,
                        RoleName = rp.Role.RoleName,
                        PermissionId = rp.PermissionId,
                        PermissionName = rp.Permission.PermissionName,
                        CreatedAt = rp.CreatedAt
                    })
                    .ToListAsync();

                if (!data.Any())
                    return NotFound(new { error = "No permissions found for this role." });

                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to fetch permissions for the role.", details = ex.Message });
            }
        }

        // -----------------------------
        // 4. Get Roles By Permission Id (Consistent DTO)
        // GET: api/rolepermission/permission/{permissionId}
        // -----------------------------
        [HttpGet("permission/{permissionId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetRolesByPermission(int permissionId)
        {
            try
            {
                var data = await _context.RolePermissions
                    .Include(rp => rp.Role)
                    .Include(rp => rp.Permission)
                    .Where(rp => rp.PermissionId == permissionId)
                    .Select(rp => new RolePermissionDTO
                    {
                        RoleId = rp.RoleId,
                        RoleName = rp.Role.RoleName,
                        PermissionId = rp.PermissionId,
                        PermissionName = rp.Permission.PermissionName,
                        CreatedAt = rp.CreatedAt
                    })
                    .ToListAsync();

                if (!data.Any())
                    return NotFound(new { error = "No roles found for this permission." });

                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to fetch roles for the permission.", details = ex.Message });
            }
        }

        // -----------------------------
        // 5. Delete (Unassign Permission from Role) - Admin only
        // DELETE: api/rolepermission
        // -----------------------------
        [HttpDelete]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RemovePermissionFromRole(CreateRolePermission dto)
        {
            try
            {
                var rolePermission = await _context.RolePermissions
                    .Include(rp => rp.Role)
                    .Include(rp => rp.Permission)
                    .FirstOrDefaultAsync(rp =>
                        rp.RoleId == dto.RoleId &&
                        rp.PermissionId == dto.PermissionId);

                if (rolePermission == null)
                    return NotFound(new { error = "Mapping not found." });

                _context.RolePermissions.Remove(rolePermission);
                await _context.SaveChangesAsync();

                return Ok(new RolePermissionDTO
                {
                    RoleId = rolePermission.RoleId,
                    RoleName = rolePermission.Role.RoleName,
                    PermissionId = rolePermission.PermissionId,
                    PermissionName = rolePermission.Permission?.PermissionName,
                    CreatedAt = rolePermission.CreatedAt
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to remove permission from role.", details = ex.Message });
            }
        }

        // -----------------------------
        // 6. Delete All Permissions of a Role - Admin only
        // DELETE: api/rolepermission/role/{roleId}
        // -----------------------------
        [HttpDelete("role/{roleId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RemoveAllPermissionsFromRole(int roleId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var mappings = await _context.RolePermissions
                    .Include(rp => rp.Role)
                    .Include(rp => rp.Permission)
                    .Where(rp => rp.RoleId == roleId)
                    .ToListAsync();

                if (!mappings.Any())
                    return NotFound(new { error = "No permissions found for this role." });

                _context.RolePermissions.RemoveRange(mappings);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var deletedDTOs = mappings.Select(rp => new RolePermissionDTO
                {
                    RoleId = rp.RoleId,
                    RoleName = rp.Role.RoleName,
                    PermissionId = rp.PermissionId,
                    PermissionName = rp.Permission?.PermissionName,
                    CreatedAt = rp.CreatedAt
                }).ToList();

                return Ok(deletedDTOs);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { error = "Failed to remove all permissions from role.", details = ex.Message });
            }
        }
    }
}
