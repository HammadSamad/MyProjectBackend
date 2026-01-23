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
    public class SpecificationDefinitionsController : ControllerBase
    {
        private readonly LaptopHarbourDbContext _context;

        public SpecificationDefinitionsController(LaptopHarbourDbContext context)
        {
            _context = context;
        }

        // ---------------------------------------------------------
        // GET: api/SpecificationDefinitions → All specifications
        // ---------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _context.SpecificationDefinitions
                .Select(s => new SpecificationDefinitionDTO
                {
                    SpecificationId = s.SpecificationId,
                    SpecificationName = s.SpecificationName,
                    DataType = s.DataType,
                    IsVariant = s.IsVariant
                })
                .ToListAsync();

            return Ok(data);
        }

        // ---------------------------------------------------------
        // GET: api/SpecificationDefinitions/{id} → Single specification
        // ---------------------------------------------------------
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var spec = await _context.SpecificationDefinitions
                .Where(s => s.SpecificationId == id)
                .Select(s => new SpecificationDefinitionDTO
                {
                    SpecificationId = s.SpecificationId,
                    SpecificationName = s.SpecificationName,
                    DataType = s.DataType,
                    IsVariant = s.IsVariant
                })
                .FirstOrDefaultAsync();

            if (spec == null)
                return NotFound("Specification not found.");

            return Ok(spec);
        }

        // ---------------------------------------------------------
        // POST: api/SpecificationDefinitions → Create specification
        // ---------------------------------------------------------
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateSpecificationDefinition model)
        {
            if (string.IsNullOrWhiteSpace(model.SpecificationName))
                return BadRequest("Specification name is required.");

            if (string.IsNullOrWhiteSpace(model.DataType))
                return BadRequest("Data type is required.");

            // Optional: Validate DataType values
            var allowedTypes = new[] { "text", "number", "bool", "option" };
            if (!allowedTypes.Contains(model.DataType.ToLower()))
                return BadRequest("Invalid DataType. Allowed: text, number, bool, option.");

            var entity = new SpecificationDefinition
            {
                SpecificationName = model.SpecificationName,
                DataType = model.DataType.ToLower(),
                IsVariant = model.IsVariant,
                CreatedAt = DateTime.UtcNow
            };

            _context.SpecificationDefinitions.Add(entity);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Specification created successfully.",
                specificationId = entity.SpecificationId
            });
        }

        // ---------------------------------------------------------
        // PUT: api/SpecificationDefinitions/{id} → Update specification
        // ---------------------------------------------------------
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] CreateSpecificationDefinition model)
        {
            var entity = await _context.SpecificationDefinitions.FindAsync(id);
            if (entity == null)
                return NotFound("Specification not found.");

            if (!string.IsNullOrWhiteSpace(model.SpecificationName))
                entity.SpecificationName = model.SpecificationName;

            if (!string.IsNullOrWhiteSpace(model.DataType))
            {
                var allowedTypes = new[] { "text", "number", "bool", "option" };
                if (!allowedTypes.Contains(model.DataType.ToLower()))
                    return BadRequest("Invalid DataType. Allowed: text, number, bool, option.");

                entity.DataType = model.DataType.ToLower();
            }

            if (model.IsVariant.HasValue)
                entity.IsVariant = model.IsVariant;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Specification updated successfully." });
        }

        // ---------------------------------------------------------
        // DELETE: api/SpecificationDefinitions/{id} → Delete specification
        // ---------------------------------------------------------
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _context.SpecificationDefinitions
                .Include(s => s.SpecificationOptions)
                .Include(s => s.ProductSpecificationValues)
                .FirstOrDefaultAsync(s => s.SpecificationId == id);

            if (entity == null)
                return NotFound("Specification not found.");

            // Remove related data first to avoid FK issues
            _context.SpecificationOptions.RemoveRange(entity.SpecificationOptions);
            _context.ProductSpecificationValues.RemoveRange(entity.ProductSpecificationValues);

            _context.SpecificationDefinitions.Remove(entity);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Specification deleted successfully." });
        }
    }
}
