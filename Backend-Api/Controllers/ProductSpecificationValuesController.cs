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
            try
            {
                var data = await _context.ProductSpecificationValues
                    .Include(psv => psv.Option)
                        .ThenInclude(o => o!.Specification)
                    .AsNoTracking()
                    .ToListAsync();

                var dto = data.Select(psv => new ProductSpecificationValueDTO
                {
                    OptionId = psv.OptionId ?? 0,
                    OptionValue = psv.Option != null && psv.Option.OptionValue != null
                                  ? psv.Option.OptionValue
                                  : string.Empty,
                    SpecificationName = psv.Option != null && psv.Option.Specification != null && psv.Option.Specification.SpecificationName != null
                                        ? psv.Option.Specification.SpecificationName
                                        : string.Empty
                }).ToList();

                return Ok(new { message = "Specification values fetched successfully.", total = dto.Count, data = dto });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"An error occurred while fetching specification values: {ex.Message}" });
            }
        }

        // ---------------------------------------------------------
        // GET: api/ProductSpecificationValues/product/{productId}
        // ---------------------------------------------------------
        [HttpGet("product/{productId:int}")]
        public async Task<IActionResult> GetByProduct(int productId)
        {
            try
            {
                var data = await _context.ProductSpecificationValues
                    .Where(x => x.ProductId == productId)
                    .Include(psv => psv.Option)
                        .ThenInclude(o => o!.Specification)
                    .AsNoTracking()
                    .ToListAsync();

                if (data == null || !data.Any())
                    return NotFound(new { message = $"No specification values found for product with ID {productId}." });

                var dto = data.Select(psv => new ProductSpecificationValueDTO
                {
                    OptionId = psv.OptionId ?? 0,
                    OptionValue = psv.Option != null && psv.Option.OptionValue != null
                                  ? psv.Option.OptionValue
                                  : string.Empty,
                    SpecificationName = psv.Option != null && psv.Option.Specification != null && psv.Option.Specification.SpecificationName != null
                                        ? psv.Option.Specification.SpecificationName
                                        : string.Empty
                }).ToList();

                return Ok(new { message = "Specification values fetched successfully.", total = dto.Count, data = dto });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"An error occurred while fetching specification values for product {productId}: {ex.Message}" });
            }
        }

        // ---------------------------------------------------------
        // POST: api/ProductSpecificationValues
        // ---------------------------------------------------------
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateProductSpecificationValue model)
        {
            if (model == null)
                return BadRequest(new { message = "Request body cannot be null." });

            try
            {
                if (model.ProductId <= 0 || model.SpecificationId <= 0)
                    return BadRequest(new { message = "Invalid product or specification ID." });

                if (!await _context.Products.AnyAsync(p => p.ProductId == model.ProductId))
                    return NotFound(new { message = "Product not found." });

                if (!await _context.SpecificationDefinitions.AnyAsync(s => s.SpecificationId == model.SpecificationId))
                    return NotFound(new { message = "Specification not found." });

                if (model.OptionId.HasValue && !await _context.SpecificationOptions.AnyAsync(o => o.OptionId == model.OptionId))
                    return NotFound(new { message = "Specification option not found." });

                // Prevent duplicate: same ProductId + SpecificationId
                if (await _context.ProductSpecificationValues
                    .AnyAsync(x => x.ProductId == model.ProductId && x.SpecificationId == model.SpecificationId))
                {
                    return BadRequest(new { message = "Specification value for this product already exists." });
                }

                var entity = new ProductSpecificationValue
                {
                    ProductId = model.ProductId,
                    SpecificationId = model.SpecificationId,
                    ValueText = model.ValueText ?? string.Empty,
                    ValueNumber = model.ValueNumber,
                    ValueBool = model.ValueBool,
                    OptionId = model.OptionId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.ProductSpecificationValues.Add(entity);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Specification value created successfully." });
            }
            catch (DbUpdateException dbEx)
            {
                return StatusCode(500, new { error = $"Database error occurred while creating specification value: {dbEx.Message}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"An error occurred while creating specification value: {ex.Message}" });
            }
        }

        // ---------------------------------------------------------
        // PUT: api/ProductSpecificationValues
        // (Composite key: ProductId + SpecificationId)
        // ---------------------------------------------------------
        [HttpPut]
        public async Task<IActionResult> Update([FromBody] CreateProductSpecificationValue model)
        {
            if (model == null)
                return BadRequest(new { message = "Request body cannot be null." });

            try
            {
                var entity = await _context.ProductSpecificationValues
                    .FirstOrDefaultAsync(x => x.ProductId == model.ProductId && x.SpecificationId == model.SpecificationId);

                if (entity == null)
                    return NotFound(new { message = "Specification value not found." });

                // Validate OptionId if provided
                if (model.OptionId.HasValue && !await _context.SpecificationOptions.AnyAsync(o => o.OptionId == model.OptionId))
                    return NotFound(new { message = "Specification option not found." });

                entity.ValueText = model.ValueText ?? string.Empty;
                entity.ValueNumber = model.ValueNumber;
                entity.ValueBool = model.ValueBool;
                entity.OptionId = model.OptionId;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Specification value updated successfully." });
            }
            catch (DbUpdateException dbEx)
            {
                return StatusCode(500, new { error = $"Database error occurred while updating specification value: {dbEx.Message}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"An error occurred while updating specification value: {ex.Message}" });
            }
        }

        // ---------------------------------------------------------
        // DELETE: api/ProductSpecificationValues/{productId}/{specificationId}
        // ---------------------------------------------------------
        [HttpDelete("{productId:int}/{specificationId:int}")]
        public async Task<IActionResult> Delete(int productId, int specificationId)
        {
            try
            {
                var entity = await _context.ProductSpecificationValues
                    .FirstOrDefaultAsync(x => x.ProductId == productId && x.SpecificationId == specificationId);

                if (entity == null)
                    return NotFound(new { message = "Specification value not found." });

                _context.ProductSpecificationValues.Remove(entity);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Specification value deleted successfully." });
            }
            catch (DbUpdateException dbEx)
            {
                return StatusCode(500, new { error = $"Database error occurred while deleting specification value: {dbEx.Message}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"An error occurred while deleting specification value: {ex.Message}" });
            }
        }
    }
}
