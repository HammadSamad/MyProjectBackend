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
    public class AddressController : ControllerBase
    {
        private readonly LaptopHarbourDbContext _context;

        public AddressController(LaptopHarbourDbContext context)
        {
            _context = context;
        }

        // ================= CREATE =================
        // POST: api/Address
        [HttpPost]
        public async Task<IActionResult> CreateAddress([FromBody] CreateAddress model)
        {
            if (model == null)
                return BadRequest(new { message = "Request body cannot be empty." });

            if (model.UserId <= 0)
                return BadRequest(new { message = "Invalid User ID." });

            if (model.CityId <= 0)
                return BadRequest(new { message = "Invalid City ID." });

            if (string.IsNullOrWhiteSpace(model.AddressLine1))
                return BadRequest(new { message = "AddressLine1 is required." });

            try
            {
                var userExists = await _context.Users.AnyAsync(u => u.UserId == model.UserId);
                if (!userExists)
                    return BadRequest(new { message = "User does not exist." });

                var cityExists = await _context.Cities.AnyAsync(c => c.CityId == model.CityId);
                if (!cityExists)
                    return BadRequest(new { message = "City does not exist." });

                // Default address per user
                if (model.IsDefault.GetValueOrDefault())
                {
                    var defaultAddresses = await _context.Addresses
                        .Where(a => a.UserId == model.UserId && a.IsDefault == true)
                        .ToListAsync();

                    foreach (var addr in defaultAddresses)
                        addr.IsDefault = false;
                }

                var address = new Address
                {
                    UserId = model.UserId,
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

        // ================= GET ALL =================
        // GET: api/Address
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AddressDTO>>> GetAllAddresses()
        {
            try
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
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to fetch addresses.", details = ex.Message });
            }
        }

        // ================= GET BY ID =================
        // GET: api/Address/5
        [HttpGet("{id}")]
        public async Task<ActionResult<AddressDTO>> GetAddressById(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid address ID." });

            try
            {
                var address = await _context.Addresses
                    .Include(a => a.City)
                    .Where(a => a.AddressId == id)
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
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to fetch address.", details = ex.Message });
            }
        }

        // ================= GET BY USER =================
        // GET: api/Address/user/5
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetAddressesByUser(int userId)
        {
            if (userId <= 0)
                return BadRequest(new { message = "Invalid User ID." });

            try
            {
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
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to fetch user addresses.", details = ex.Message });
            }
        }

        // ================= UPDATE =================
        // PUT: api/Address/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAddress(int id, [FromBody] CreateAddress model)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid address ID." });

            if (model == null)
                return BadRequest(new { message = "Request body cannot be empty." });

            if (model.CityId <= 0)
                return BadRequest(new { message = "Invalid City ID." });

            if (string.IsNullOrWhiteSpace(model.AddressLine1))
                return BadRequest(new { message = "AddressLine1 is required." });

            try
            {
                var address = await _context.Addresses.FindAsync(id);
                if (address == null)
                    return NotFound(new { message = "Address not found." });

                // Default per user
                if (model.IsDefault.GetValueOrDefault())
                {
                    var defaultAddresses = await _context.Addresses
                        .Where(a => a.UserId == address.UserId && a.IsDefault == true && a.AddressId != id)
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

                return Ok(new
                {
                    message = "Address updated successfully.",
                    addressId = address.AddressId,
                    userId = address.UserId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Unexpected error occurred.", details = ex.Message });
            }
        }

        // ================= DELETE =================
        // DELETE: api/Address/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAddress(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid address ID." });

            try
            {
                var address = await _context.Addresses.FindAsync(id);
                if (address == null)
                    return NotFound(new { message = "Address not found." });

                _context.Addresses.Remove(address);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Address deleted successfully.",
                    addressId = address.AddressId,
                    userId = address.UserId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Unexpected error occurred.", details = ex.Message });
            }
        }
    }
}
