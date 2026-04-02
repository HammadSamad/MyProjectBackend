using Backend_Api.Data;
using Backend_Api.Helpers;
using Backend_Api.Models;
using Backend_Api.Models.Model_Create;
using Backend_Api.Models.Model_DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Route("api/admin/users")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminUserController : ControllerBase
{
    private readonly LaptopHarbourDbContext _context;
    private readonly PasswordHasherHelper _passwordHasher;

    public AdminUserController(LaptopHarbourDbContext context, PasswordHasherHelper passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    // -------------------------
    // Admin creates a user
    // -------------------------
    [HttpPost]
    public async Task<IActionResult> CreateUser(Signup dto)
    {
        if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
            return BadRequest(new { message = "Email already exists." });

        var user = new User
        {
            Username = dto.Username,
            Email = dto.Email,
            PasswordHash = _passwordHasher.HashPassword(dto.Password),
            IsActive = true,
            IsEmailVerified = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok(new { message = "User created successfully.", userId = user.UserId });
    }

    // -------------------------
    // Update User
    // -------------------------
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(int id, UserDTO dto)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound(new { message = "User not found." });

        user.FirstName = dto.FirstName;
        user.LastName = dto.LastName;
        user.PhoneNumber = dto.PhoneNumber;
        user.IsActive = dto.IsActive;

        await _context.SaveChangesAsync();
        return Ok(new { message = "User updated successfully." });
    }

    // -------------------------
    // Delete User
    // -------------------------
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound(new { message = "User not found." });

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        return Ok(new { message = "User deleted successfully." });
    }

    // -------------------------
    // Assign Roles (multiple roles)
    // -------------------------
    [HttpPost("assign-roles")]
    public async Task<IActionResult> AssignRoles(AssignRoleDTO dto)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.UserId == dto.UserId);

        if (user == null)
            return NotFound(new { message = "User not found." });

        foreach (var roleName in dto.Roles!)
        {
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName);
            if (role == null) continue;

            if (!user.UserRoles.Any(ur => ur.RoleId == role.RoleId))
            {
                _context.UserRoles.Add(new UserRole
                {
                    UserId = dto.UserId,
                    RoleId = role.RoleId
                });
            }
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "Roles assigned successfully." });
    }

    // -------------------------
    // Remove Roles (multiple roles)
    // -------------------------
    [HttpPost("remove-roles")]
    public async Task<IActionResult> RemoveRoles(AssignRoleDTO dto)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.UserId == dto.UserId);

        if (user == null)
            return NotFound(new { message = "User not found." });

        foreach (var roleName in dto.Roles!)
        {
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName);
            if (role == null) continue;

            var userRole = user.UserRoles.FirstOrDefault(ur => ur.RoleId == role.RoleId);
            if (userRole != null)
                _context.UserRoles.Remove(userRole);
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "Roles removed successfully." });
    }
}
