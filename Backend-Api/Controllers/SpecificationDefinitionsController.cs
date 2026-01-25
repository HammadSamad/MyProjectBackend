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
            try
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
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to fetch specifications.", details = ex.Message });
            }
        }

        // ---------------------------------------------------------
        // GET: api/SpecificationDefinitions/{id} → Single specification
        // ---------------------------------------------------------
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
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
                    return NotFound(new { error = "Specification not found." });

                return Ok(spec);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to fetch specification.", details = ex.Message });
            }
        }

        // ---------------------------------------------------------
        // POST: api/SpecificationDefinitions → Create specification
        // ---------------------------------------------------------
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateSpecificationDefinition model)
        {
            try
            {
                var name = model.SpecificationName?.Trim() ?? "";
                if (string.IsNullOrWhiteSpace(name))
                    return BadRequest(new { error = "Specification name is required." });

                if (string.IsNullOrWhiteSpace(model.DataType))
                    return BadRequest(new { error = "Data type is required." });

                // Optional: Validate DataType values
                var allowedTypes = new[] { "text", "number", "bool", "option" };
                if (!allowedTypes.Contains(model.DataType.ToLower()))
                    return BadRequest(new { error = "Invalid DataType. Allowed: text, number, bool, option." });

                // Check for duplicate (case-insensitive)
                if (await _context.SpecificationDefinitions
                    .AnyAsync(s => s.SpecificationName != null &&
                                   s.SpecificationName.Equals(name, StringComparison.OrdinalIgnoreCase)))
                {
                    return BadRequest(new { error = "Specification name already exists." });
                }

                var entity = new SpecificationDefinition
                {
                    SpecificationName = name,
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
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to create specification.", details = ex.Message });
            }
        }

        // ---------------------------------------------------------
        // PUT: api/SpecificationDefinitions/{id} → Update specification
        // ---------------------------------------------------------
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] CreateSpecificationDefinition model)
        {
            try
            {
                var entity = await _context.SpecificationDefinitions.FindAsync(id);
                if (entity == null)
                    return NotFound(new { error = "Specification not found." });

                // Check duplicate if name is changing
                var newName = model.SpecificationName?.Trim();
                if (!string.IsNullOrWhiteSpace(newName) &&
                    !newName.Equals(entity.SpecificationName, StringComparison.OrdinalIgnoreCase))
                {
                    if (await _context.SpecificationDefinitions
                        .AnyAsync(s => s.SpecificationName != null &&
                                       s.SpecificationName.Equals(newName, StringComparison.OrdinalIgnoreCase)))
                    {
                        return BadRequest(new { error = "Specification name already exists." });
                    }

                    entity.SpecificationName = newName;
                }

                if (!string.IsNullOrWhiteSpace(model.DataType))
                {
                    var allowedTypes = new[] { "text", "number", "bool", "option" };
                    if (!allowedTypes.Contains(model.DataType.ToLower()))
                        return BadRequest(new { error = "Invalid DataType. Allowed: text, number, bool, option." });

                    entity.DataType = model.DataType.ToLower();
                }

                if (model.IsVariant.HasValue)
                    entity.IsVariant = model.IsVariant;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Specification updated successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to update specification.", details = ex.Message });
            }
        }

        // ---------------------------------------------------------
        // DELETE: api/SpecificationDefinitions/{id} → Delete specification
        // ---------------------------------------------------------
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var entity = await _context.SpecificationDefinitions
                    .Include(s => s.SpecificationOptions)
                    .Include(s => s.ProductSpecificationValues)
                    .FirstOrDefaultAsync(s => s.SpecificationId == id);

                if (entity == null)
                    return NotFound(new { error = "Specification not found." });

                // Remove related data first
                _context.SpecificationOptions.RemoveRange(entity.SpecificationOptions);
                _context.ProductSpecificationValues.RemoveRange(entity.ProductSpecificationValues);
                _context.SpecificationDefinitions.Remove(entity);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { message = "Specification deleted successfully." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { error = "Failed to delete specification.", details = ex.Message });
            }
        }
    }
}
