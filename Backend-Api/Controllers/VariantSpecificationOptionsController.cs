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

        // -------------------------------------------------------------
        // GET: api/VariantSpecificationOptions → All variant specifications
        // -------------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> GetAll()
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

        // -------------------------------------------------------------
        // GET: api/VariantSpecificationOptions/variant/{variantId} → Variant options
        // -------------------------------------------------------------
        [HttpGet("variant/{variantId:int}")]
        public async Task<IActionResult> GetByVariant(int variantId)
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

            return Ok(data);
        }

        // -------------------------------------------------------------
        // POST: api/VariantSpecificationOptions → Add variant specification
        // -------------------------------------------------------------
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateVariantSpecificationOption model)
        {
            // Validate variant
            var variantExists = await _context.ProductVariants
                .AnyAsync(v => v.VariantId == model.VariantId);
            if (!variantExists)
                return NotFound("Variant not found.");

            // Validate option
            var optionExists = await _context.SpecificationOptions
                .AnyAsync(o => o.OptionId == model.OptionId);
            if (!optionExists)
                return NotFound("Specification option not found.");

            // Check duplicate
            var exists = await _context.VariantSpecificationOptions
                .AnyAsync(vso => vso.VariantId == model.VariantId && vso.OptionId == model.OptionId);
            if (exists)
                return BadRequest("This option is already assigned to this variant.");

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

        // -------------------------------------------------------------
        // PUT: api/VariantSpecificationOptions/{variantId}/{optionId} → Update variant specification
        // -------------------------------------------------------------
        [HttpPut("{variantId:int}/{optionId:int}")]
        public async Task<IActionResult> Update(int variantId, int optionId, [FromBody] CreateVariantSpecificationOption model)
        {
            var vso = await _context.VariantSpecificationOptions
                .FirstOrDefaultAsync(v => v.VariantId == variantId && v.OptionId == optionId);

            if (vso == null)
                return NotFound("Variant specification option not found.");

            // Only allow changing OptionId to a different option
            if (model.OptionId != optionId)
            {
                var optionExists = await _context.SpecificationOptions.AnyAsync(o => o.OptionId == model.OptionId);
                if (!optionExists)
                    return NotFound("New specification option not found.");

                // Check duplicate
                var duplicate = await _context.VariantSpecificationOptions
                    .AnyAsync(v => v.VariantId == variantId && v.OptionId == model.OptionId);
                if (duplicate)
                    return BadRequest("This option is already assigned to this variant.");

                vso.OptionId = model.OptionId;
            }

            vso.CreatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Variant specification option updated successfully." });
        }

        // -------------------------------------------------------------
        // DELETE: api/VariantSpecificationOptions/{variantId}/{optionId} → Delete variant specification
        // -------------------------------------------------------------
        [HttpDelete("{variantId:int}/{optionId:int}")]
        public async Task<IActionResult> Delete(int variantId, int optionId)
        {
            var vso = await _context.VariantSpecificationOptions
                .FirstOrDefaultAsync(v => v.VariantId == variantId && v.OptionId == optionId);

            if (vso == null)
                return NotFound("Variant specification option not found.");

            _context.VariantSpecificationOptions.Remove(vso);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Variant specification option deleted successfully." });
        }
    }
}
