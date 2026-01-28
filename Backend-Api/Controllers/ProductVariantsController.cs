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

        // ================= HELPER =================
        private decimal CalculateFinalPrice(ProductVariant v)
        {
            if (v.Price == null) return 0;

            var now = DateTime.UtcNow;

            if (v.DiscountStart.HasValue && v.DiscountEnd.HasValue)
            {
                if (now < v.DiscountStart || now > v.DiscountEnd)
                    return v.Price.Value;
            }

            decimal finalPrice = v.Price.Value;

            if (v.DiscountPercentage.HasValue && v.DiscountPercentage > 0)
                finalPrice -= finalPrice * (v.DiscountPercentage.Value / 100);
            else if (v.DiscountAmount.HasValue && v.DiscountAmount > 0)
                finalPrice -= v.DiscountAmount.Value;

            return finalPrice < 0 ? 0 : finalPrice;
        }

        // =====================================================
        // GET: api/ProductVariants
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> GetAllVariants()
        {
            try
            {
                var variants = await _context.ProductVariants
                    .AsNoTracking()
                    .Include(v => v.VariantSpecificationOptions)
                        .ThenInclude(vso => vso.Option)
                            .ThenInclude(o => o.Specification)
                    .ToListAsync();

                if (!variants.Any())
                    return NotFound("No variants found.");

                var dtoList = variants.Select(v => new ProductVariantDTO
                {
                    VariantId = v.VariantId,
                    Sku = v.Sku,
                    Price = v.Price,
                    FinalPrice = CalculateFinalPrice(v),
                    Stock = v.Stock,
                    DiscountPercentage = v.DiscountPercentage,
                    DiscountAmount = v.DiscountAmount,
                    DiscountStart = v.DiscountStart,
                    DiscountEnd = v.DiscountEnd,
                    Specifications = v.VariantSpecificationOptions
                        .Select(vso => new VariantSpecificationOptionDTO
                        {
                            SpecificationName = vso.Option?.Specification?.SpecificationName ?? string.Empty,
                            OptionValue = vso.Option?.OptionValue ?? string.Empty
                        }).ToList()
                }).ToList();

                return Ok(dtoList);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to fetch variants: {ex.Message}");
            }
        }

        // =====================================================
        // GET: api/ProductVariants/{id}
        // =====================================================
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetVariantById(int id)
        {
            try
            {
                var v = await _context.ProductVariants
                    .AsNoTracking()
                    .Include(v => v.VariantSpecificationOptions)
                        .ThenInclude(vso => vso.Option)
                            .ThenInclude(o => o.Specification)
                    .FirstOrDefaultAsync(v => v.VariantId == id);

                if (v == null)
                    return NotFound($"Variant with ID {id} not found.");

                var dto = new ProductVariantDTO
                {
                    VariantId = v.VariantId,
                    Sku = v.Sku,
                    Price = v.Price,
                    FinalPrice = CalculateFinalPrice(v),
                    Stock = v.Stock,
                    DiscountPercentage = v.DiscountPercentage,
                    DiscountAmount = v.DiscountAmount,
                    DiscountStart = v.DiscountStart,
                    DiscountEnd = v.DiscountEnd,
                    Specifications = v.VariantSpecificationOptions
                        .Select(vso => new VariantSpecificationOptionDTO
                        {
                            SpecificationName = vso.Option?.Specification?.SpecificationName ?? string.Empty,
                            OptionValue = vso.Option?.OptionValue ?? string.Empty
                        }).ToList()
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to fetch variant: {ex.Message}");
            }
        }

        // =====================================================
        // POST: api/ProductVariants
        // =====================================================
        [HttpPost]
        public async Task<IActionResult> CreateVariant(CreateProductVariant model)
        {
            try
            {
                if (model.Price <= 0)
                    return BadRequest("Price must be greater than 0.");

                if (model.DiscountPercentage.HasValue && model.DiscountAmount.HasValue)
                    return BadRequest("Use either DiscountPercentage or DiscountAmount, not both.");

                if (!await _context.Products.AnyAsync(p => p.ProductId == model.ProductId))
                    return NotFound($"Product with ID {model.ProductId} not found.");

                var variant = new ProductVariant
                {
                    ProductId = model.ProductId,
                    Sku = model.Sku,
                    Price = model.Price,
                    Stock = model.Stock,
                    DiscountPercentage = model.DiscountPercentage,
                    DiscountAmount = model.DiscountAmount,
                    DiscountStart = model.DiscountStart,
                    DiscountEnd = model.DiscountEnd,
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
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to create variant: {ex.Message}");
            }
        }

        // =====================================================
        // PUT: api/ProductVariants/{id}
        // =====================================================
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateVariant(int id, CreateProductVariant model)
        {
            try
            {
                var variant = await _context.ProductVariants.FindAsync(id);
                if (variant == null)
                    return NotFound($"Variant with ID {id} not found.");

                if (model.DiscountPercentage.HasValue && model.DiscountAmount.HasValue)
                    return BadRequest("Use either DiscountPercentage or DiscountAmount, not both.");

                variant.Sku = model.Sku;
                variant.Price = model.Price;
                variant.Stock = model.Stock;
                variant.DiscountPercentage = model.DiscountPercentage;
                variant.DiscountAmount = model.DiscountAmount;
                variant.DiscountStart = model.DiscountStart;
                variant.DiscountEnd = model.DiscountEnd;
                variant.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Variant updated successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to update variant: {ex.Message}");
            }
        }

        // =====================================================
        // DELETE: api/ProductVariants/{id}
        // =====================================================
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteVariant(int id)
        {
            try
            {
                var variant = await _context.ProductVariants
                    .Include(v => v.VariantSpecificationOptions)
                    .Include(v => v.VariantPriceHistories)
                    .FirstOrDefaultAsync(v => v.VariantId == id);

                if (variant == null)
                    return NotFound($"Variant with ID {id} not found.");

                _context.VariantSpecificationOptions.RemoveRange(variant.VariantSpecificationOptions);
                _context.VariantPriceHistories.RemoveRange(variant.VariantPriceHistories);
                _context.ProductVariants.Remove(variant);

                await _context.SaveChangesAsync();

                return Ok(new { message = "Variant deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to delete variant: {ex.Message}");
            }
        }
    }
}
