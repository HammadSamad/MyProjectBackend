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
    public class SpecificationOptionsController : ControllerBase
    {
        private readonly LaptopHarbourDbContext _context;

        public SpecificationOptionsController(LaptopHarbourDbContext context)
        {
            _context = context;
        }

        // ---------------------------------------------------------
        // GET: api/SpecificationOptions → All options
        // ---------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _context.SpecificationOptions
                .Select(o => new SpecificationOptionDTO
                {
                    OptionId = o.OptionId,
                    OptionValue = o.OptionValue
                })
                .ToListAsync();

            return Ok(data);
        }

        // ---------------------------------------------------------
        // GET: api/SpecificationOptions/{id} → Single option
        // ---------------------------------------------------------
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var option = await _context.SpecificationOptions
                .Where(o => o.OptionId == id)
                .Select(o => new SpecificationOptionDTO
                {
                    OptionId = o.OptionId,
                    OptionValue = o.OptionValue
                })
                .FirstOrDefaultAsync();

            if (option == null)
                return NotFound("Specification option not found.");

            return Ok(option);
        }

        // ---------------------------------------------------------
        // GET: api/SpecificationOptions/specification/{specificationId}
        // ---------------------------------------------------------
        [HttpGet("specification/{specificationId:int}")]
        public async Task<IActionResult> GetBySpecification(int specificationId)
        {
            var options = await _context.SpecificationOptions
                .Where(o => o.SpecificationId == specificationId)
                .Select(o => new SpecificationOptionDTO
                {
                    OptionId = o.OptionId,
                    OptionValue = o.OptionValue
                })
                .ToListAsync();

            return Ok(options);
        }

        // ---------------------------------------------------------
        // POST: api/SpecificationOptions → Create option
        // ---------------------------------------------------------
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateSpecificationOption model)
        {
            if (model.SpecificationId <= 0)
                return BadRequest("Invalid specification ID.");

            if (string.IsNullOrWhiteSpace(model.OptionValue))
                return BadRequest("Option value is required.");

            var specExists = await _context.SpecificationDefinitions
                .AnyAsync(s => s.SpecificationId == model.SpecificationId);
            if (!specExists)
                return NotFound("Specification not found.");

            var option = new SpecificationOption
            {
                SpecificationId = model.SpecificationId,
                OptionValue = model.OptionValue,
                CreatedAt = DateTime.UtcNow
            };

            _context.SpecificationOptions.Add(option);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Specification option created successfully.",
                optionId = option.OptionId
            });
        }

        // ---------------------------------------------------------
        // PUT: api/SpecificationOptions/{id} → Update option
        // ---------------------------------------------------------
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] CreateSpecificationOption model)
        {
            var option = await _context.SpecificationOptions.FindAsync(id);
            if (option == null)
                return NotFound("Specification option not found.");

            if (!string.IsNullOrWhiteSpace(model.OptionValue))
                option.OptionValue = model.OptionValue;

            if (model.SpecificationId > 0 && model.SpecificationId != option.SpecificationId)
            {
                var specExists = await _context.SpecificationDefinitions
                    .AnyAsync(s => s.SpecificationId == model.SpecificationId);
                if (!specExists)
                    return NotFound("Specification not found.");

                option.SpecificationId = model.SpecificationId;
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Specification option updated successfully." });
        }

        // ---------------------------------------------------------
        // DELETE: api/SpecificationOptions/{id} → Delete option
        // ---------------------------------------------------------
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var option = await _context.SpecificationOptions
                .Include(o => o.ProductSpecificationValues)
                .Include(o => o.VariantSpecificationOptions)
                .FirstOrDefaultAsync(o => o.OptionId == id);

            if (option == null)
                return NotFound("Specification option not found.");

            // Remove dependencies first
            _context.ProductSpecificationValues.RemoveRange(option.ProductSpecificationValues);
            _context.VariantSpecificationOptions.RemoveRange(option.VariantSpecificationOptions);

            _context.SpecificationOptions.Remove(option);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Specification option deleted successfully." });
        }
    }
}
