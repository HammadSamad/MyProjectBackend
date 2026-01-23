using Backend_Api.Data;
using Backend_Api.Models;
using Backend_Api.Models.Model_DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend_Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductSpecificationValuesController : ControllerBase
    {
        private readonly LaptopHarbourDbContext _context;

        public ProductSpecificationValuesController(LaptopHarbourDbContext context)
        {
            _context = context;
        }

        // ---------------------------------------------------------
        // GET: api/ProductSpecificationValues
        // ---------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _context.ProductSpecificationValues
                .Include(psv => psv.Option)
                    .ThenInclude(o => o.Specification)
                .Select(psv => new ProductSpecificationValueDTO
                {
                    OptionId = psv.OptionId ?? 0,
                    OptionValue = psv.Option!.OptionValue!,
                    SpecificationName = psv.Option!.Specification.SpecificationName!
                })
                .ToListAsync();

            return Ok(data);
        }

        // ---------------------------------------------------------
        // GET: api/ProductSpecificationValues/{productId}
        // ---------------------------------------------------------
        [HttpGet("product/{productId:int}")]
        public async Task<IActionResult> GetByProduct(int productId)
        {
            var data = await _context.ProductSpecificationValues
                .Where(x => x.ProductId == productId)
                .Include(psv => psv.Option)
                    .ThenInclude(o => o.Specification)
                .Select(psv => new ProductSpecificationValueDTO
                {
                    OptionId = psv.OptionId ?? 0,
                    OptionValue = psv.Option!.OptionValue!,
                    SpecificationName = psv.Option!.Specification.SpecificationName!
                })
                .ToListAsync();

            return Ok(data);
        }

        // ---------------------------------------------------------
        // POST: api/ProductSpecificationValues
        // ---------------------------------------------------------
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateProductSpecificationValue model)
        {
            if (model.ProductId <= 0 || model.SpecificationId <= 0)
                return BadRequest("Invalid product or specification.");

            var productExists = await _context.Products.AnyAsync(p => p.ProductId == model.ProductId);
            if (!productExists)
                return NotFound("Product not found.");

            var specificationExists = await _context.SpecificationDefinitions
                .AnyAsync(s => s.SpecificationId == model.SpecificationId);
            if (!specificationExists)
                return NotFound("Specification not found.");

            if (model.OptionId.HasValue)
            {
                var optionExists = await _context.SpecificationOptions
                    .AnyAsync(o => o.OptionId == model.OptionId);
                if (!optionExists)
                    return NotFound("Specification option not found.");
            }

            var entity = new ProductSpecificationValue
            {
                ProductId = model.ProductId,
                SpecificationId = model.SpecificationId,
                ValueText = model.ValueText,
                ValueNumber = model.ValueNumber,
                ValueBool = model.ValueBool,
                OptionId = model.OptionId,
                CreatedAt = DateTime.UtcNow
            };

            _context.ProductSpecificationValues.Add(entity);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Specification value created successfully." });
        }

        // ---------------------------------------------------------
        // PUT: api/ProductSpecificationValues
        // (Composite key: ProductId + SpecificationId)
        // ---------------------------------------------------------
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] CreateProductSpecificationValue model)
        {
            var entity = await _context.ProductSpecificationValues
                .FirstOrDefaultAsync(x =>
                    x.ProductId == model.ProductId &&
                    x.SpecificationId == model.SpecificationId);

            if (entity == null)
                return NotFound("Specification value not found.");

            entity.ValueText = model.ValueText;
            entity.ValueNumber = model.ValueNumber;
            entity.ValueBool = model.ValueBool;
            entity.OptionId = model.OptionId;

            _context.ProductSpecificationValues.Update(entity);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Specification value updated successfully." });
        }

        // ---------------------------------------------------------
        // DELETE: api/ProductSpecificationValues/{productId}/{specificationId}
        // ---------------------------------------------------------
        [HttpDelete("{productId:int}/{specificationId:int}")]
        public async Task<IActionResult> Delete(int productId, int specificationId)
        {
            var entity = await _context.ProductSpecificationValues
                .FirstOrDefaultAsync(x =>
                    x.ProductId == productId &&
                    x.SpecificationId == specificationId);

            if (entity == null)
                return NotFound("Specification value not found.");

            _context.ProductSpecificationValues.Remove(entity);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Specification value deleted successfully." });
        }
    }
}
