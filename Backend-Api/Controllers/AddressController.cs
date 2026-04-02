using Backend_Api.Data;
using Backend_Api.Models;
using Backend_Api.Models.Model_Create;
using Backend_Api.Models.Model_DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Backend_Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // 🔐 JWT applied
    public class AddressController : ControllerBase
    {
        private readonly LaptopHarbourDbContext _context;

        public AddressController(LaptopHarbourDbContext context)
        {
            _context = context;
        }

        // ================= JWT HELPER =================
        private bool TryGetUserId(out int userId)
        {
            userId = 0;
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return !string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out userId);
        }

        // ================= CREATE =================
        [HttpPost]
        public async Task<IActionResult> CreateAddress([FromBody] CreateAddress model)
        {
            if (!TryGetUserId(out int userId))
                return Unauthorized(new { message = "Invalid token." });

            if (model == null)
                return BadRequest(new { message = "Request body cannot be empty." });

            if (model.CityId <= 0)
                return BadRequest(new { message = "Invalid City ID." });

            if (string.IsNullOrWhiteSpace(model.AddressLine1))
                return BadRequest(new { message = "AddressLine1 is required." });

            try
            {
                var cityExists = await _context.Cities.AnyAsync(c => c.CityId == model.CityId);
                if (!cityExists)
                    return BadRequest(new { message = "City does not exist." });

                if (model.IsDefault.GetValueOrDefault())
                {
                    var defaultAddresses = await _context.Addresses
                        .Where(a => a.UserId == userId && a.IsDefault == true)
                        .ToListAsync();

                    foreach (var addr in defaultAddresses)
                        addr.IsDefault = false;
                }

                var address = new Address
                {
                    UserId = userId,
                    CityId = model.CityId,
                    AddressLine1 = model.AddressLine1,
                    AddressLine2 = model.AddressLine2,
                    PostalCode = model.PostalCode,
                    IsDefault = model.IsDefault.GetValueOrDefault(),
                    CreatedAt = DateTime.UtcNow
                };

                _context.Addresses.Add(address);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Address created successfully.",
                    addressId = address.AddressId,
                    userId = address.UserId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Unexpected error occurred.", details = ex.Message });
            }
        }

        // ================= GET ALL (ADMIN ONLY) =================
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AddressDTO>>> GetAllAddresses()
        {
            var addresses = await _context.Addresses
                .Include(a => a.City)
                .Select(a => new AddressDTO
                {
                    AddressId = a.AddressId,
                    UserId = a.UserId,
                    CityId = a.CityId,
                    CityName = a.City.CityName,
                    AddressLine1 = a.AddressLine1,
                    AddressLine2 = a.AddressLine2,
                    PostalCode = a.PostalCode,
                    IsDefault = a.IsDefault.GetValueOrDefault()
                })
                .ToListAsync();

            if (!addresses.Any())
                return NotFound(new { message = "No addresses found." });

            return Ok(addresses);
        }

        // ================= GET BY ID =================
        [HttpGet("{id}")]
        public async Task<ActionResult<AddressDTO>> GetAddressById(int id)
        {
            if (!TryGetUserId(out int userId))
                return Unauthorized();

            if (id <= 0)
                return BadRequest(new { message = "Invalid address ID." });

            var address = await _context.Addresses
                .Include(a => a.City)
                .Where(a => a.AddressId == id && a.UserId == userId)
                .Select(a => new AddressDTO
                {
                    AddressId = a.AddressId,
                    UserId = a.UserId,
                    CityId = a.CityId,
                    CityName = a.City.CityName,
                    AddressLine1 = a.AddressLine1,
                    AddressLine2 = a.AddressLine2,
                    PostalCode = a.PostalCode,
                    IsDefault = a.IsDefault.GetValueOrDefault()
                })
                .FirstOrDefaultAsync();

            if (address == null)
                return NotFound(new { message = "Address not found." });

            return Ok(address);
        }

        // ================= GET MY ADDRESSES =================
        [HttpGet("me")]
        public async Task<IActionResult> GetMyAddresses()
        {
            if (!TryGetUserId(out int userId))
                return Unauthorized();

            var addresses = await _context.Addresses
                .Include(a => a.City)
                .Where(a => a.UserId == userId)
                .Select(a => new AddressDTO
                {
                    AddressId = a.AddressId,
                    UserId = a.UserId,
                    CityId = a.CityId,
                    CityName = a.City.CityName,
                    AddressLine1 = a.AddressLine1,
                    AddressLine2 = a.AddressLine2,
                    PostalCode = a.PostalCode,
                    IsDefault = a.IsDefault.GetValueOrDefault()
                })
                .ToListAsync();

            if (!addresses.Any())
                return NotFound(new { message = "No addresses found for this user." });

            return Ok(addresses);
        }

        // ================= UPDATE =================
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAddress(int id, [FromBody] CreateAddress model)
        {
            if (!TryGetUserId(out int userId))
                return Unauthorized();

            if (id <= 0)
                return BadRequest(new { message = "Invalid address ID." });

            if (model == null)
                return BadRequest(new { message = "Request body cannot be empty." });

            if (model.CityId <= 0)
                return BadRequest(new { message = "Invalid City ID." });

            if (string.IsNullOrWhiteSpace(model.AddressLine1))
                return BadRequest(new { message = "AddressLine1 is required." });

            var address = await _context.Addresses
                .FirstOrDefaultAsync(a => a.AddressId == id && a.UserId == userId);

            if (address == null)
                return NotFound(new { message = "Address not found." });

            if (model.IsDefault.GetValueOrDefault())
            {
                var defaultAddresses = await _context.Addresses
                    .Where(a => a.UserId == userId && a.IsDefault == true && a.AddressId != id)
                    .ToListAsync();

                foreach (var addr in defaultAddresses)
                    addr.IsDefault = false;
            }

            address.CityId = model.CityId;
            address.AddressLine1 = model.AddressLine1;
            address.AddressLine2 = model.AddressLine2;
            address.PostalCode = model.PostalCode;
            address.IsDefault = model.IsDefault.GetValueOrDefault();
            address.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Address updated successfully." });
        }

        // ================= DELETE =================
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAddress(int id)
        {
            if (!TryGetUserId(out int userId))
                return Unauthorized();

            if (id <= 0)
                return BadRequest(new { message = "Invalid address ID." });

            var address = await _context.Addresses
                .FirstOrDefaultAsync(a => a.AddressId == id && a.UserId == userId);

            if (address == null)
                return NotFound(new { message = "Address not found." });

            _context.Addresses.Remove(address);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Address deleted successfully." });
        }
    }
}