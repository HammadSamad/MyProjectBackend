using Backend_Api.Data;
using Backend_Api.Models;
using Backend_Api.Models.Model_Create;
using Backend_Api.Models.Model_DTO;
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
        // 1. Create Role
        // POST: api/role
        // -----------------------------
        [HttpPost]
        public async Task<IActionResult> CreateRole(CreateRole dto)
        {
            if (await _context.Roles.AnyAsync(r => r.RoleName == dto.RoleName))
                return BadRequest("Role already exists.");

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

        // -----------------------------
        // 2. Get All Roles
        // GET: api/role
        // -----------------------------
        [HttpGet]
        public async Task<IActionResult> GetAllRoles()
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

        // -----------------------------
        // 3. Get Role By Id
        // GET: api/role/{id}
        // -----------------------------
        [HttpGet("{id}")]
        public async Task<IActionResult> GetRoleById(int id)
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
                return NotFound("Role not found.");

            return Ok(role);
        }

        // -----------------------------
        // 4. Update Role
        // PUT: api/role/{id}
        // -----------------------------
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRole(int id, CreateRole dto)
        {
            var role = await _context.Roles.FindAsync(id);

            if (role == null)
                return NotFound("Role not found.");

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

        // -----------------------------
        // 5. Delete Role
        // DELETE: api/role/{id}
        // -----------------------------
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRole(int id)
        {
            var role = await _context.Roles
                .Include(r => r.RolePermissions)
                .Include(r => r.UserRoles)
                .FirstOrDefaultAsync(r => r.RoleId == id);

            if (role == null)
                return NotFound("Role not found.");

            // Remove dependencies first (important)
            _context.RolePermissions.RemoveRange(role.RolePermissions);
            _context.UserRoles.RemoveRange(role.UserRoles);

            _context.Roles.Remove(role);
            await _context.SaveChangesAsync();

            return Ok("Role deleted successfully.");
        }

        // ---------------------------------------------------
        // 6. Get Roles with Their Permissions (Recommended)
        // GET: api/role/with-permissions
        // ---------------------------------------------------
        [HttpGet("with-permissions")]
        public async Task<IActionResult> GetRolesWithPermissions()
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

        // ---------------------------------------------------
        // 7. Get Single Role with Its Permissions
        // GET: api/role/{id}/with-permissions
        // ---------------------------------------------------
        [HttpGet("{id}/with-permissions")]
        public async Task<IActionResult> GetRoleWithPermissions(int id)
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
                return NotFound("Role not found.");

            return Ok(role);
        }
    }
}
