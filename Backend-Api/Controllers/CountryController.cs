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
    public class CountryController : ControllerBase
    {
        private readonly LaptopHarbourDbContext _context;

        public CountryController(LaptopHarbourDbContext context)
        {
            _context = context;
        }

        // CREATE
        [HttpPost]
        public async Task<IActionResult> CreateCountry([FromBody] CountryDTO model)
        {
            if (string.IsNullOrWhiteSpace(model.CountryName))
                return BadRequest("Country name is required.");

            var country = new Country
            {
                CountryName = model.CountryName,
                CreatedAt = DateTime.UtcNow
            };

            _context.Countries.Add(country);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Country created successfully", country.CountryId });
        }

        // GET ALL
        [HttpGet]
        public async Task<IActionResult> GetCountries()
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

        // GET BY ID
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetCountryById(int id)
        {
            var country = await _context.Countries
                .Where(c => c.CountryId == id)
                .Select(c => new CountryDTO
                {
                    CountryId = c.CountryId,
                    CountryName = c.CountryName
                })
                .FirstOrDefaultAsync();

            if (country == null) return NotFound("Country not found");
            return Ok(country);
        }

        // UPDATE
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateCountry(int id, [FromBody] CountryDTO model)
        {
            var country = await _context.Countries.FindAsync(id);
            if (country == null) return NotFound("Country not found");

            country.CountryName = model.CountryName;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Country updated successfully" });
        }

        // DELETE
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteCountry(int id)
        {
            var country = await _context.Countries.FindAsync(id);
            if (country == null) return NotFound("Country not found");

            _context.Countries.Remove(country);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Country deleted successfully" });
        }
    }
}
