using Backend_Api.Data;
using Backend_Api.Models;
using Backend_Api.Models.Model_Create;
using Backend_Api.Models.Model_DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;

namespace Backend_Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoleController : ControllerBase
    {
        private readonly LaptopHarbourDbContext _context;

        public RoleController(LaptopHarbourDbContext context)
        {
            _context = context;
        }

        // -----------------------------
        // 1. Create Role (Admin only)
        // POST: api/role
        // -----------------------------
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateRole(CreateRole dto)
        {
            try
            {
                if (await _context.Roles.AnyAsync(r => r.RoleName == dto.RoleName))
                    return BadRequest(new { error = "Role already exists." });

                var role = new Role
                {
                    RoleName = dto.RoleName,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Roles.Add(role);
                await _context.SaveChangesAsync();

                return Ok(new RoleDTO
                {
                    RoleId = role.RoleId,
                    RoleName = role.RoleName,
                    CreatedAt = role.CreatedAt,
                    UpdatedAt = role.UpdatedAt
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to create role.", details = ex.Message });
            }
        }

        // -----------------------------
        // 2. Get All Roles (Anyone can view)
        // GET: api/role
        // -----------------------------
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllRoles()
        {
            try
            {
                var roles = await _context.Roles
                    .Select(r => new RoleDTO
                    {
                        RoleId = r.RoleId,
                        RoleName = r.RoleName,
                        CreatedAt = r.CreatedAt,
                        UpdatedAt = r.UpdatedAt
                    })
                    .ToListAsync();

                return Ok(roles);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to fetch roles.", details = ex.Message });
            }
        }

        // -----------------------------
        // 3. Get Role By Id (Anyone can view)
        // GET: api/role/{id}
        // -----------------------------
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetRoleById(int id)
        {
            try
            {
                var role = await _context.Roles
                    .Where(r => r.RoleId == id)
                    .Select(r => new RoleDTO
                    {
                        RoleId = r.RoleId,
                        RoleName = r.RoleName,
                        CreatedAt = r.CreatedAt,
                        UpdatedAt = r.UpdatedAt
                    })
                    .FirstOrDefaultAsync();

                if (role == null)
                    return NotFound(new { error = "Role not found." });

                return Ok(role);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to fetch role.", details = ex.Message });
            }
        }

        // -----------------------------
        // 4. Update Role (Admin only)
        // PUT: api/role/{id}
        // -----------------------------
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateRole(int id, CreateRole dto)
        {
            try
            {
                var role = await _context.Roles.FindAsync(id);
                if (role == null)
                    return NotFound(new { error = "Role not found." });

                // Prevent duplicate role name
                bool duplicate = await _context.Roles
                    .AnyAsync(r => r.RoleName == dto.RoleName && r.RoleId != id);
                if (duplicate)
                    return BadRequest(new { error = "Role name already in use by another role." });

                role.RoleName = dto.RoleName;
                role.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new RoleDTO
                {
                    RoleId = role.RoleId,
                    RoleName = role.RoleName,
                    CreatedAt = role.CreatedAt,
                    UpdatedAt = role.UpdatedAt
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to update role.", details = ex.Message });
            }
        }

        // -----------------------------
        // 5. Delete Role (Admin only)
        // DELETE: api/role/{id}
        // -----------------------------
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteRole(int id)
        {
            // Protect critical roles
            var protectedRoles = new[] { "Admin", "SuperAdmin" };

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var role = await _context.Roles
                    .Include(r => r.RolePermissions)
                    .Include(r => r.UserRoles)
                    .FirstOrDefaultAsync(r => r.RoleId == id);

                if (role == null)
                    return NotFound(new { error = "Role not found." });

                if (protectedRoles.Contains(role.RoleName))
                    return BadRequest(new { error = $"Cannot delete protected role: {role.RoleName}" });

                // Remove dependencies first
                _context.RolePermissions.RemoveRange(role.RolePermissions);
                _context.UserRoles.RemoveRange(role.UserRoles);

                _context.Roles.Remove(role);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return Ok(new { message = "Role deleted successfully." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { error = "Failed to delete role.", details = ex.Message });
            }
        }

        // ---------------------------------------------------
        // 6. Get Roles with Their Permissions (Anyone)
        // GET: api/role/with-permissions
        // ---------------------------------------------------
        [HttpGet("with-permissions")]
        [AllowAnonymous]
        public async Task<IActionResult> GetRolesWithPermissions()
        {
            try
            {
                var roles = await _context.Roles
                    .Include(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
                    .Select(r => new
                    {
                        r.RoleId,
                        r.RoleName,
                        r.CreatedAt,
                        r.UpdatedAt,
                        Permissions = r.RolePermissions.Select(rp => new
                        {
                            rp.PermissionId,
                            rp.Permission.PermissionName,
                            rp.Permission.PermissionPath,
                            rp.CreatedAt
                        }).ToList()
                    })
                    .ToListAsync();

                return Ok(roles);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to fetch roles with permissions.", details = ex.Message });
            }
        }

        // ---------------------------------------------------
        // 7. Get Single Role with Its Permissions (Anyone)
        // GET: api/role/{id}/with-permissions
        // ---------------------------------------------------
        [HttpGet("{id}/with-permissions")]
        [AllowAnonymous]
        public async Task<IActionResult> GetRoleWithPermissions(int id)
        {
            try
            {
                var role = await _context.Roles
                    .Include(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
                    .Where(r => r.RoleId == id)
                    .Select(r => new
                    {
                        r.RoleId,
                        r.RoleName,
                        r.CreatedAt,
                        r.UpdatedAt,
                        Permissions = r.RolePermissions.Select(rp => new
                        {
                            rp.PermissionId,
                            rp.Permission.PermissionName,
                            rp.Permission.PermissionPath,
                            rp.CreatedAt
                        }).ToList()
                    })
                    .FirstOrDefaultAsync();

                if (role == null)
                    return NotFound(new { error = "Role not found." });

                return Ok(role);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to fetch role with permissions.", details = ex.Message });
            }
        }
    }
}
