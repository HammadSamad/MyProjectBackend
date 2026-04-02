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
    public class VariantSpecificationOptionsController : ControllerBase
    {
        private readonly LaptopHarbourDbContext _context;

        public VariantSpecificationOptionsController(LaptopHarbourDbContext context)
        {
            _context = context;
        }

        // GET: api/VariantSpecificationOptions
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var data = await _context.VariantSpecificationOptions
                    .Include(vso => vso.Option)
                        .ThenInclude(o => o.Specification)
                    .Select(vso => new VariantSpecificationOptionDTO
                    {
                        SpecificationName = vso.Option.Specification.SpecificationName,
                        OptionValue = vso.Option.OptionValue
                    })
                    .ToListAsync();

                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve variant specification options.", details = ex.Message });
            }
        }

        // GET: api/VariantSpecificationOptions/variant/{variantId}
        [HttpGet("variant/{variantId:int}")]
        public async Task<IActionResult> GetByVariant(int variantId)
        {
            try
            {
                var data = await _context.VariantSpecificationOptions
                    .Where(vso => vso.VariantId == variantId)
                    .Include(vso => vso.Option)
                        .ThenInclude(o => o.Specification)
                    .Select(vso => new VariantSpecificationOptionDTO
                    {
                        SpecificationName = vso.Option.Specification.SpecificationName,
                        OptionValue = vso.Option.OptionValue
                    })
                    .ToListAsync();

                if (data.Count == 0)
                    return NotFound(new { error = "No variant specification options found for this variant." });

                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve variant specification options for the variant.", details = ex.Message });
            }
        }

        // POST: api/VariantSpecificationOptions
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateVariantSpecificationOption model)
        {
            if (model == null)
                return BadRequest(new { error = "Request body cannot be empty." });

            try
            {
                // Validate variant
                var variantExists = await _context.ProductVariants.AnyAsync(v => v.VariantId == model.VariantId);
                if (!variantExists)
                    return NotFound(new { error = "Variant not found." });

                // Validate option
                var optionExists = await _context.SpecificationOptions.AnyAsync(o => o.OptionId == model.OptionId);
                if (!optionExists)
                    return NotFound(new { error = "Specification option not found." });

                // Check duplicate
                var exists = await _context.VariantSpecificationOptions
                    .AnyAsync(vso => vso.VariantId == model.VariantId && vso.OptionId == model.OptionId);
                if (exists)
                    return BadRequest(new { error = "This option is already assigned to this variant." });

                var vsoEntity = new VariantSpecificationOption
                {
                    VariantId = model.VariantId,
                    OptionId = model.OptionId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.VariantSpecificationOptions.Add(vsoEntity);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Variant specification option added successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to create variant specification option.", details = ex.Message });
            }
        }

        // PUT: api/VariantSpecificationOptions/{variantId}/{optionId}
        [HttpPut("{variantId:int}/{optionId:int}")]
        public async Task<IActionResult> Update(int variantId, int optionId, [FromBody] CreateVariantSpecificationOption model)
        {
            if (model == null)
                return BadRequest(new { error = "Request body cannot be empty." });

            try
            {
                var vso = await _context.VariantSpecificationOptions
                    .FirstOrDefaultAsync(v => v.VariantId == variantId && v.OptionId == optionId);

                if (vso == null)
                    return NotFound(new { error = "Variant specification option not found." });

                if (model.OptionId != optionId)
                {
                    var optionExists = await _context.SpecificationOptions.AnyAsync(o => o.OptionId == model.OptionId);
                    if (!optionExists)
                        return NotFound(new { error = "New specification option not found." });

                    var duplicate = await _context.VariantSpecificationOptions
                        .AnyAsync(v => v.VariantId == variantId && v.OptionId == model.OptionId);
                    if (duplicate)
                        return BadRequest(new { error = "This option is already assigned to this variant." });

                    vso.OptionId = model.OptionId;
                }

                vso.CreatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Variant specification option updated successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to update variant specification option.", details = ex.Message });
            }
        }

        // DELETE: api/VariantSpecificationOptions/{variantId}/{optionId}
        [HttpDelete("{variantId:int}/{optionId:int}")]
        public async Task<IActionResult> Delete(int variantId, int optionId)
        {
            try
            {
                var vso = await _context.VariantSpecificationOptions
                    .FirstOrDefaultAsync(v => v.VariantId == variantId && v.OptionId == optionId);

                if (vso == null)
                    return NotFound(new { error = "Variant specification option not found." });

                _context.VariantSpecificationOptions.Remove(vso);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Variant specification option deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to delete variant specification option.", details = ex.Message });
            }
        }
    }
}
