using Backend_Api.Data;
using Backend_Api.Models;
using Backend_Api.Models.Model_DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend_Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductVariantsController : ControllerBase
    {
        private readonly LaptopHarbourDbContext _context;

        public ProductVariantsController(LaptopHarbourDbContext context)
        {
            _context = context;
        }

        // ---------------------------------------------------------
        // GET: api/ProductVariants  → All variants
        // ---------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> GetAllVariants()
        {
            var variants = await _context.ProductVariants
                .Include(v => v.VariantSpecificationOptions)
                    .ThenInclude(vso => vso.Option)
                        .ThenInclude(o => o.Specification)
                .Select(v => new ProductVariantDTO
                {
                    VariantId = v.VariantId,
                    Sku = v.Sku,
                    Price = v.Price,
                    Stock = v.Stock,
                    Specifications = v.VariantSpecificationOptions
                        .Select(vso => new VariantSpecificationOptionDTO
                        {
                            SpecificationName = vso.Option.Specification.SpecificationName,
                            OptionValue = vso.Option.OptionValue
                        })
                        .ToList()
                })
                .ToListAsync();

            return Ok(variants);
        }

        // ---------------------------------------------------------
        // GET: api/ProductVariants/{id} → Single variant
        // ---------------------------------------------------------
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetVariantById(int id)
        {
            var variant = await _context.ProductVariants
                .Include(v => v.VariantSpecificationOptions)
                    .ThenInclude(vso => vso.Option)
                        .ThenInclude(o => o.Specification)
                .Where(v => v.VariantId == id)
                .Select(v => new ProductVariantDTO
                {
                    VariantId = v.VariantId,
                    Sku = v.Sku,
                    Price = v.Price,
                    Stock = v.Stock,
                    Specifications = v.VariantSpecificationOptions
                        .Select(vso => new VariantSpecificationOptionDTO
                        {
                            SpecificationName = vso.Option.Specification.SpecificationName,
                            OptionValue = vso.Option.OptionValue
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync();

            if (variant == null)
                return NotFound("Variant not found.");

            return Ok(variant);
        }

        // ---------------------------------------------------------
        // GET: api/ProductVariants/product/{productId}
        // ---------------------------------------------------------
        [HttpGet("product/{productId:int}")]
        public async Task<IActionResult> GetVariantsByProduct(int productId)
        {
            var variants = await _context.ProductVariants
                .Where(v => v.ProductId == productId)
                .Include(v => v.VariantSpecificationOptions)
                    .ThenInclude(vso => vso.Option)
                        .ThenInclude(o => o.Specification)
                .Select(v => new ProductVariantDTO
                {
                    VariantId = v.VariantId,
                    Sku = v.Sku,
                    Price = v.Price,
                    Stock = v.Stock,
                    Specifications = v.VariantSpecificationOptions
                        .Select(vso => new VariantSpecificationOptionDTO
                        {
                            SpecificationName = vso.Option.Specification.SpecificationName,
                            OptionValue = vso.Option.OptionValue
                        })
                        .ToList()
                })
                .ToListAsync();

            return Ok(variants);
        }

        // ---------------------------------------------------------
        // POST: api/ProductVariants → Create variant
        // ---------------------------------------------------------
        [HttpPost]
        public async Task<IActionResult> CreateVariant([FromBody] CreateProductVariant model)
        {
            if (model == null || model.ProductId <= 0)
                return BadRequest("Invalid data.");

            var productExists = await _context.Products.AnyAsync(p => p.ProductId == model.ProductId);
            if (!productExists)
                return NotFound("Product not found.");

            var variant = new ProductVariant
            {
                ProductId = model.ProductId,
                Sku = model.Sku,
                Price = model.Price,
                Stock = model.Stock,
                CreatedAt = DateTime.UtcNow
            };

            _context.ProductVariants.Add(variant);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetVariantById), new { id = variant.VariantId }, new
            {
                message = "Variant created successfully",
                variantId = variant.VariantId
            });
        }

        // ---------------------------------------------------------
        // PUT: api/ProductVariants/{id} → Update variant
        // ---------------------------------------------------------
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateVariant(int id, [FromBody] CreateProductVariant model)
        {
            var variant = await _context.ProductVariants.FindAsync(id);
            if (variant == null)
                return NotFound("Variant not found.");

            variant.Sku = model.Sku;
            variant.Price = model.Price;
            variant.Stock = model.Stock;
            variant.UpdatedAt = DateTime.UtcNow;

            _context.ProductVariants.Update(variant);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Variant updated successfully." });
        }

        // ---------------------------------------------------------
        // DELETE: api/ProductVariants/{id} → Delete variant
        // ---------------------------------------------------------
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteVariant(int id)
        {
            var variant = await _context.ProductVariants
                .Include(v => v.VariantSpecificationOptions)
                .Include(v => v.VariantPriceHistories)
                .FirstOrDefaultAsync(v => v.VariantId == id);

            if (variant == null)
                return NotFound("Variant not found.");

            // Remove related data first
            _context.VariantSpecificationOptions.RemoveRange(variant.VariantSpecificationOptions);
            _context.VariantPriceHistories.RemoveRange(variant.VariantPriceHistories);

            _context.ProductVariants.Remove(variant);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Variant deleted successfully." });
        }
    }
}
