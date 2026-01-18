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
    public class PermissionController : ControllerBase
    {
        private readonly LaptopHarbourDbContext _context;

        public PermissionController(LaptopHarbourDbContext context)
        {
            _context = context;
        }

        // -----------------------------
        // 1. Create Permission
        // POST: api/permission
        // -----------------------------
        [HttpPost]
        public async Task<IActionResult> CreatePermission(CreatePermission dto)
        {
            if (await _context.Permissions.AnyAsync(p => p.PermissionName == dto.PermissionName))
                return BadRequest("Permission already exists.");

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

        // -----------------------------
        // 2. Get All Permissions
        // GET: api/permission
        // -----------------------------
        [HttpGet]
        public async Task<IActionResult> GetAllPermissions()
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

            return Ok(permissions);
        }

        // -----------------------------
        // 3. Get Permission By Id
        // GET: api/permission/{id}
        // -----------------------------
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPermissionById(int id)
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
                return NotFound("Permission not found.");

            return Ok(permission);
        }

        // -----------------------------
        // 4. Update Permission
        // PUT: api/permission/{id}
        // -----------------------------
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePermission(int id, CreatePermission dto)
        {
            var permission = await _context.Permissions.FindAsync(id);

            if (permission == null)
                return NotFound("Permission not found.");

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

        // -----------------------------
        // 5. Delete Permission
        // DELETE: api/permission/{id}
        // -----------------------------
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePermission(int id)
        {
            var permission = await _context.Permissions
                .Include(p => p.RolePermissions)
                .FirstOrDefaultAsync(p => p.PermissionId == id);

            if (permission == null)
                return NotFound("Permission not found.");

            // Remove Role-Permission mappings first
            _context.RolePermissions.RemoveRange(permission.RolePermissions);

            _context.Permissions.Remove(permission);
            await _context.SaveChangesAsync();

            return Ok("Permission deleted successfully.");
        }

        // ---------------------------------------------------
        // 6. Get Permissions with Roles (Optional but useful)
        // GET: api/permission/with-roles
        // ---------------------------------------------------
        [HttpGet("with-roles")]
        public async Task<IActionResult> GetPermissionsWithRoles()
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

            return Ok(permissions);
        }

        // ---------------------------------------------------
        // 7. Get Single Permission with Roles
        // GET: api/permission/{id}/with-roles
        // ---------------------------------------------------
        [HttpGet("{id}/with-roles")]
        public async Task<IActionResult> GetPermissionWithRoles(int id)
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
                return NotFound("Permission not found.");

            return Ok(permission);
        }
    }
}
