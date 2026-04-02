using Backend_Api.Data;
using Backend_Api.Models;
using Backend_Api.Models.Model_Create;
using Backend_Api.Models.Model_DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Backend_Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PermissionController : ControllerBase
    {
        private readonly LaptopHarbourDbContext _context;

        public PermissionController(LaptopHarbourDbContext context)
        {
            _context = context;
        }

        // -----------------------------
        // 1. Create Permission
        // -----------------------------
        [HttpPost]
        public async Task<IActionResult> CreatePermission(CreatePermission dto)
        {
            try
            {
                if (dto == null || string.IsNullOrWhiteSpace(dto.PermissionName))
                    return BadRequest(new { error = "PermissionName is required." });

                // Prevent duplicate
                bool exists = await _context.Permissions
                    .AnyAsync(p => p.PermissionName.ToLower() == dto.PermissionName.ToLower());
                if (exists)
                    return BadRequest(new { error = "Permission already exists." });

                var permission = new Permission
                {
                    PermissionName = dto.PermissionName,
                    PermissionPath = dto.PermissionPath,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Permissions.Add(permission);
                await _context.SaveChangesAsync();

                return Ok(new PermissionDTO
                {
                    PermissionId = permission.PermissionId,
                    PermissionName = permission.PermissionName,
                    PermissionPath = permission.PermissionPath,
                    CreatedAt = permission.CreatedAt
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to create permission.", details = ex.Message });
            }
        }

        // -----------------------------
        // 2. Get All Permissions
        // -----------------------------
        [HttpGet]
        public async Task<IActionResult> GetAllPermissions()
        {
            try
            {
                var permissions = await _context.Permissions
                    .Select(p => new PermissionDTO
                    {
                        PermissionId = p.PermissionId,
                        PermissionName = p.PermissionName,
                        PermissionPath = p.PermissionPath,
                        CreatedAt = p.CreatedAt
                    })
                    .ToListAsync();

                if (permissions.Count == 0)
                    return Ok(new { message = "No permissions found.", data = new List<PermissionDTO>() });

                return Ok(permissions);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to fetch permissions.", details = ex.Message });
            }
        }

        // -----------------------------
        // 3. Get Permission By Id
        // -----------------------------
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPermissionById(int id)
        {
            try
            {
                var permission = await _context.Permissions
                    .Where(p => p.PermissionId == id)
                    .Select(p => new PermissionDTO
                    {
                        PermissionId = p.PermissionId,
                        PermissionName = p.PermissionName,
                        PermissionPath = p.PermissionPath,
                        CreatedAt = p.CreatedAt
                    })
                    .FirstOrDefaultAsync();

                if (permission == null)
                    return NotFound(new { error = "Permission not found." });

                return Ok(permission);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to fetch permission.", details = ex.Message });
            }
        }

        // -----------------------------
        // 4. Update Permission
        // -----------------------------
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePermission(int id, CreatePermission dto)
        {
            try
            {
                if (dto == null || string.IsNullOrWhiteSpace(dto.PermissionName))
                    return BadRequest(new { error = "PermissionName is required." });

                var permission = await _context.Permissions.FindAsync(id);
                if (permission == null)
                    return NotFound(new { error = "Permission not found." });

                // Prevent duplicate name on update
                bool duplicateExists = await _context.Permissions
                    .AnyAsync(p => p.PermissionId != id && p.PermissionName.ToLower() == dto.PermissionName.ToLower());
                if (duplicateExists)
                    return BadRequest(new { error = "Another permission with the same name already exists." });

                permission.PermissionName = dto.PermissionName;
                permission.PermissionPath = dto.PermissionPath;
                permission.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new PermissionDTO
                {
                    PermissionId = permission.PermissionId,
                    PermissionName = permission.PermissionName,
                    PermissionPath = permission.PermissionPath,
                    CreatedAt = permission.CreatedAt
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to update permission.", details = ex.Message });
            }
        }

        // -----------------------------
        // 5. Delete Permission
        // -----------------------------
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePermission(int id)
        {
            try
            {
                var permission = await _context.Permissions
                    .Include(p => p.RolePermissions)
                    .FirstOrDefaultAsync(p => p.PermissionId == id);

                if (permission == null)
                    return NotFound(new { error = "Permission not found." });

                // Remove Role-Permission mappings first
                _context.RolePermissions.RemoveRange(permission.RolePermissions);

                _context.Permissions.Remove(permission);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Permission deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to delete permission.", details = ex.Message });
            }
        }

        // -----------------------------
        // 6. Get Permissions with Roles
        // -----------------------------
        [HttpGet("with-roles")]
        public async Task<IActionResult> GetPermissionsWithRoles()
        {
            try
            {
                var permissions = await _context.Permissions
                    .Include(p => p.RolePermissions)
                        .ThenInclude(rp => rp.Role)
                    .Select(p => new
                    {
                        p.PermissionId,
                        p.PermissionName,
                        p.PermissionPath,
                        p.CreatedAt,
                        Roles = p.RolePermissions.Select(rp => new
                        {
                            rp.RoleId,
                            rp.Role.RoleName,
                            rp.CreatedAt
                        }).ToList()
                    })
                    .ToListAsync();

                if (permissions.Count == 0)
                    return Ok(new { message = "No permissions found.", data = permissions });

                return Ok(permissions);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to fetch permissions with roles.", details = ex.Message });
            }
        }

        // -----------------------------
        // 7. Get Single Permission with Roles
        // -----------------------------
        [HttpGet("{id}/with-roles")]
        public async Task<IActionResult> GetPermissionWithRoles(int id)
        {
            try
            {
                var permission = await _context.Permissions
                    .Include(p => p.RolePermissions)
                        .ThenInclude(rp => rp.Role)
                    .Where(p => p.PermissionId == id)
                    .Select(p => new
                    {
                        p.PermissionId,
                        p.PermissionName,
                        p.PermissionPath,
                        p.CreatedAt,
                        Roles = p.RolePermissions.Select(rp => new
                        {
                            rp.RoleId,
                            rp.Role.RoleName,
                            rp.CreatedAt
                        }).ToList()
                    })
                    .FirstOrDefaultAsync();

                if (permission == null)
                    return NotFound(new { error = "Permission not found." });

                return Ok(permission);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to fetch permission with roles.", details = ex.Message });
            }
        }
    }
}
