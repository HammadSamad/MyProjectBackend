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
            try
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
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to fetch specification options.", details = ex.Message });
            }
        }

        // ---------------------------------------------------------
        // GET: api/SpecificationOptions/{id} → Single option
        // ---------------------------------------------------------
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
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
                    return NotFound(new { error = "Specification option not found." });

                return Ok(option);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to fetch specification option.", details = ex.Message });
            }
        }

        // ---------------------------------------------------------
        // GET: api/SpecificationOptions/specification/{specificationId}
        // ---------------------------------------------------------
        [HttpGet("specification/{specificationId:int}")]
        public async Task<IActionResult> GetBySpecification(int specificationId)
        {
            try
            {
                var options = await _context.SpecificationOptions
                    .Where(o => o.SpecificationId == specificationId)
                    .Select(o => new SpecificationOptionDTO
                    {
                        OptionId = o.OptionId,
                        OptionValue = o.OptionValue
                    })
                    .ToListAsync();

                if (!options.Any())
                    return NotFound(new { error = "No options found for this specification." });

                return Ok(options);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to fetch specification options.", details = ex.Message });
            }
        }

        // ---------------------------------------------------------
        // POST: api/SpecificationOptions → Create option
        // ---------------------------------------------------------
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateSpecificationOption model)
        {
            try
            {
                if (model.SpecificationId <= 0)
                    return BadRequest(new { error = "Invalid specification ID." });

                var optionValue = model.OptionValue?.Trim() ?? "";
                if (string.IsNullOrWhiteSpace(optionValue))
                    return BadRequest(new { error = "Option value is required." });

                var specExists = await _context.SpecificationDefinitions
                    .AnyAsync(s => s.SpecificationId == model.SpecificationId);
                if (!specExists)
                    return NotFound(new { error = "Specification not found." });

                // Optional: prevent duplicate option value for the same specification
                if (await _context.SpecificationOptions
                    .AnyAsync(o => o.SpecificationId == model.SpecificationId &&
                                   o.OptionValue != null &&
                                   o.OptionValue.Equals(optionValue, StringComparison.OrdinalIgnoreCase)))
                {
                    return BadRequest(new { error = "Option value already exists for this specification." });
                }

                var option = new SpecificationOption
                {
                    SpecificationId = model.SpecificationId,
                    OptionValue = optionValue,
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
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to create specification option.", details = ex.Message });
            }
        }

        // ---------------------------------------------------------
        // PUT: api/SpecificationOptions/{id} → Update option
        // ---------------------------------------------------------
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] CreateSpecificationOption model)
        {
            try
            {
                var option = await _context.SpecificationOptions.FindAsync(id);
                if (option == null)
                    return NotFound(new { error = "Specification option not found." });

                if (!string.IsNullOrWhiteSpace(model.OptionValue))
                {
                    var valueTrimmed = model.OptionValue.Trim();
                    // Optional: prevent duplicate value
                    if (await _context.SpecificationOptions
                        .AnyAsync(o => o.SpecificationId == option.SpecificationId &&
                                       o.OptionId != id &&
                                       o.OptionValue != null &&
                                       o.OptionValue.Equals(valueTrimmed, StringComparison.OrdinalIgnoreCase)))
                    {
                        return BadRequest(new { error = "Option value already exists for this specification." });
                    }

                    option.OptionValue = valueTrimmed;
                }

                if (model.SpecificationId > 0 && model.SpecificationId != option.SpecificationId)
                {
                    var specExists = await _context.SpecificationDefinitions
                        .AnyAsync(s => s.SpecificationId == model.SpecificationId);
                    if (!specExists)
                        return NotFound(new { error = "Specification not found." });

                    option.SpecificationId = model.SpecificationId;
                }

                await _context.SaveChangesAsync();

                return Ok(new { message = "Specification option updated successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to update specification option.", details = ex.Message });
            }
        }

        // ---------------------------------------------------------
        // DELETE: api/SpecificationOptions/{id} → Delete option
        // ---------------------------------------------------------
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var option = await _context.SpecificationOptions
                    .Include(o => o.ProductSpecificationValues)
                    .Include(o => o.VariantSpecificationOptions)
                    .FirstOrDefaultAsync(o => o.OptionId == id);

                if (option == null)
                    return NotFound(new { error = "Specification option not found." });

                // Remove dependencies first
                _context.ProductSpecificationValues.RemoveRange(option.ProductSpecificationValues);
                _context.VariantSpecificationOptions.RemoveRange(option.VariantSpecificationOptions);
                _context.SpecificationOptions.Remove(option);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { message = "Specification option deleted successfully." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { error = "Failed to delete specification option.", details = ex.Message });
            }
        }
    }
}
