using Backend_Api.Data;
using Backend_Api.Models;
using Backend_Api.Models.Model_DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend_Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CityController : ControllerBase
    {
        private readonly LaptopHarbourDbContext _context;

        public CityController(LaptopHarbourDbContext context)
        {
            _context = context;
        }

        // CREATE
        [HttpPost]
        public async Task<IActionResult> CreateCity([FromBody] CityDTO model)
        {
            if (string.IsNullOrWhiteSpace(model.CityName))
                return BadRequest("City name is required.");

            if (!await _context.Countries.AnyAsync(c => c.CountryId == model.CountryId))
                return BadRequest("Invalid CountryId.");

            var city = new City
            {
                CityName = model.CityName,
                CountryId = model.CountryId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Cities.Add(city);
            await _context.SaveChangesAsync();

            return Ok(new { message = "City created successfully", city.CityId });
        }

        // GET ALL
        [HttpGet]
        public async Task<IActionResult> GetCities()
        {
            var cities = await _context.Cities
                .Select(c => new CityDTO
                {
                    CityId = c.CityId,
                    CityName = c.CityName,
                    CountryId = c.CountryId
                })
                .ToListAsync();

            return Ok(cities);
        }

        // GET BY ID
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetCityById(int id)
        {
            var city = await _context.Cities
                .Where(c => c.CityId == id)
                .Select(c => new CityDTO
                {
                    CityId = c.CityId,
                    CityName = c.CityName,
                    CountryId = c.CountryId
                })
                .FirstOrDefaultAsync();

            if (city == null) return NotFound("City not found");
            return Ok(city);
        }

        // UPDATE
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateCity(int id, [FromBody] CityDTO model)
        {
            var city = await _context.Cities.FindAsync(id);
            if (city == null) return NotFound("City not found");

            city.CityName = model.CityName;
            city.CountryId = model.CountryId;

            await _context.SaveChangesAsync();
            return Ok(new { message = "City updated successfully" });
        }

        // DELETE
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteCity(int id)
        {
            var city = await _context.Cities.FindAsync(id);
            if (city == null) return NotFound("City not found");

            _context.Cities.Remove(city);
            await _context.SaveChangesAsync();

            return Ok(new { message = "City deleted successfully" });
        }
    }
}
