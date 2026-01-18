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
            var address = new Address
            {
                CityId = model.CityId,
                AddressLine1 = model.AddressLine1,
                AddressLine2 = model.AddressLine2,
                PostalCode = model.PostalCode,
                IsDefault = model.IsDefault,
                CreatedAt = DateTime.UtcNow
            };

            _context.Addresses.Add(address);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Address created successfully", addressId = address.AddressId });
        }

        // ================= GET ALL =================
        // GET: api/Address
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AddressDTO>>> GetAllAddresses()
        {
            var addresses = await _context.Addresses
                .Include(a => a.City)
                .Select(a => new AddressDTO
                {
                    AddressId = a.AddressId,
                    CityId = a.CityId,
                    CityName = a.City.CityName,
                    AddressLine1 = a.AddressLine1,
                    AddressLine2 = a.AddressLine2,
                    PostalCode = a.PostalCode,
                    IsDefault = a.IsDefault
                })
                .ToListAsync();

            return Ok(addresses);
        }

        // ================= GET BY ID =================
        // GET: api/Address/5
        [HttpGet("{id}")]
        public async Task<ActionResult<AddressDTO>> GetAddressById(int id)
        {
            var address = await _context.Addresses
                .Include(a => a.City)
                .Where(a => a.AddressId == id)
                .Select(a => new AddressDTO
                {
                    AddressId = a.AddressId,
                    CityId = a.CityId,
                    CityName = a.City.CityName,
                    AddressLine1 = a.AddressLine1,
                    AddressLine2 = a.AddressLine2,
                    PostalCode = a.PostalCode,
                    IsDefault = a.IsDefault
                })
                .FirstOrDefaultAsync();

            if (address == null)
                return NotFound("Address not found");

            return Ok(address);
        }

        // ================= UPDATE =================
        // PUT: api/Address/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAddress(int id, [FromBody] CreateAddress model)
        {
            var address = await _context.Addresses.FindAsync(id);

            if (address == null)
                return NotFound("Address not found");

            address.CityId = model.CityId;
            address.AddressLine1 = model.AddressLine1;
            address.AddressLine2 = model.AddressLine2;
            address.PostalCode = model.PostalCode;
            address.IsDefault = model.IsDefault;
            address.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Address updated successfully" });
        }

        // ================= DELETE =================
        // DELETE: api/Address/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAddress(int id)
        {
            var address = await _context.Addresses.FindAsync(id);

            if (address == null)
                return NotFound("Address not found");

            _context.Addresses.Remove(address);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Address deleted successfully" });
        }
    }
}
