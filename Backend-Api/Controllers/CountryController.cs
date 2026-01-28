using Backend_Api.Data;
using Backend_Api.Models;
using Backend_Api.Models.Model_DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend_Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CountryController : ControllerBase
    {
        private readonly LaptopHarbourDbContext _context;

        public CountryController(LaptopHarbourDbContext context)
        {
            _context = context;
        }

        // ================= CREATE =================
        [HttpPost]
        public async Task<IActionResult> CreateCountry([FromBody] CountryDTO model)
        {
            try
            {
                if (model == null || string.IsNullOrWhiteSpace(model.CountryName))
                    return BadRequest(new { error = "Country name is required." });

                // Check for duplicate country name (case-insensitive)
                var exists = await _context.Countries
                    .AnyAsync(c => c.CountryName!.ToLower() == model.CountryName.ToLower());
                if (exists)
                    return Conflict(new { error = "Country name already exists." });

                var country = new Country
                {
                    CountryName = model.CountryName,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Countries.Add(country);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Country created successfully", country.CountryId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to create country", details = ex.Message });
            }
        }

        // ================= GET ALL =================
        [HttpGet]
        public async Task<IActionResult> GetCountries()
        {
            try
            {
                var countries = await _context.Countries
                    .Select(c => new CountryDTO
                    {
                        CountryId = c.CountryId,
                        CountryName = c.CountryName
                    })
                    .ToListAsync();

                return Ok(countries);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to fetch countries", details = ex.Message });
            }
        }

        // ================= GET BY ID =================
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetCountryById(int id)
        {
            try
            {
                var country = await _context.Countries
                    .Where(c => c.CountryId == id)
                    .Select(c => new CountryDTO
                    {
                        CountryId = c.CountryId,
                        CountryName = c.CountryName
                    })
                    .FirstOrDefaultAsync();

                if (country == null)
                    return NotFound(new { error = "Country not found" });

                return Ok(country);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to fetch country", details = ex.Message });
            }
        }

        // ================= UPDATE =================
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateCountry(int id, [FromBody] CountryDTO model)
        {
            try
            {
                if (model == null || string.IsNullOrWhiteSpace(model.CountryName))
                    return BadRequest(new { error = "Country name is required." });

                var country = await _context.Countries.FindAsync(id);
                if (country == null)
                    return NotFound(new { error = "Country not found" });

                // Check for duplicate country name (case-insensitive), excluding current record
                var exists = await _context.Countries
                    .AnyAsync(c => c.CountryId != id && c.CountryName!.ToLower() == model.CountryName.ToLower());
                if (exists)
                    return Conflict(new { error = "Country name already exists." });

                country.CountryName = model.CountryName;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Country updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to update country", details = ex.Message });
            }
        }

        // ================= DELETE =================
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteCountry(int id)
        {
            try
            {
                var country = await _context.Countries.FindAsync(id);
                if (country == null)
                    return NotFound(new { error = "Country not found" });

                _context.Countries.Remove(country);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Country deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to delete country", details = ex.Message });
            }
        }
    }
}
