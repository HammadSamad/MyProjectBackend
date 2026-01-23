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

        // ================= CREATE =================
        [HttpPost]
        public async Task<IActionResult> CreateCity([FromBody] CityDTO model)
        {
            try
            {
                if (model == null || string.IsNullOrWhiteSpace(model.CityName))
                    return BadRequest(new { message = "City name is required." });

                // Validate country
                bool countryExists = await _context.Countries.AnyAsync(c => c.CountryId == model.CountryId);
                if (!countryExists)
                    return BadRequest(new { message = "Invalid CountryId." });

                // Check for duplicate city name within the same country
                bool duplicateCity = await _context.Cities
                    .AnyAsync(c => c.CountryId == model.CountryId &&
                                   c.CityName.ToLower() == model.CityName.ToLower());
                if (duplicateCity)
                    return Conflict(new { message = "City name already exists in this country." });

                var city = new City
                {
                    CityName = model.CityName,
                    CountryId = model.CountryId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Cities.Add(city);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "City created successfully.",
                    cityId = city.CityId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while creating the city.",
                    error = ex.Message
                });
            }
        }

        // ================= GET ALL =================
        [HttpGet]
        public async Task<IActionResult> GetCities()
        {
            try
            {
                var cities = await _context.Cities
                    .Select(c => new CityDTO
                    {
                        CityId = c.CityId,
                        CityName = c.CityName,
                        CountryId = c.CountryId
                    })
                    .ToListAsync();

                if (cities.Count == 0)
                    return Ok(new { message = "No cities found.", data = new List<CityDTO>() });

                return Ok(cities);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while fetching cities.",
                    error = ex.Message
                });
            }
        }

        // ================= GET BY ID =================
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetCityById(int id)
        {
            try
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

                if (city == null)
                    return NotFound(new { message = "City not found." });

                return Ok(city);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while fetching the city.",
                    error = ex.Message
                });
            }
        }

        // ================= UPDATE =================
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateCity(int id, [FromBody] CityDTO model)
        {
            try
            {
                if (model == null || string.IsNullOrWhiteSpace(model.CityName))
                    return BadRequest(new { message = "City name is required." });

                var city = await _context.Cities.FindAsync(id);
                if (city == null)
                    return NotFound(new { message = "City not found." });

                // Validate country
                bool countryExists = await _context.Countries.AnyAsync(c => c.CountryId == model.CountryId);
                if (!countryExists)
                    return BadRequest(new { message = "Invalid CountryId." });

                // Check for duplicate city name within the same country, excluding current city
                bool duplicateCity = await _context.Cities
                    .AnyAsync(c => c.CityId != id &&
                                   c.CountryId == model.CountryId &&
                                   c.CityName.ToLower() == model.CityName.ToLower());
                if (duplicateCity)
                    return Conflict(new { message = "City name already exists in this country." });

                city.CityName = model.CityName;
                city.CountryId = model.CountryId;
                city.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new { message = "City updated successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while updating the city.",
                    error = ex.Message
                });
            }
        }

        // ================= DELETE =================
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteCity(int id)
        {
            try
            {
                var city = await _context.Cities.FindAsync(id);
                if (city == null)
                    return NotFound(new { message = "City not found." });

                _context.Cities.Remove(city);
                await _context.SaveChangesAsync();

                return Ok(new { message = "City deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while deleting the city.",
                    error = ex.Message
                });
            }
        }
    }
}
