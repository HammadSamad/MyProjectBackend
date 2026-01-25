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
    public class VariantPriceHistoryController : ControllerBase
    {
        private readonly LaptopHarbourDbContext _context;

        public VariantPriceHistoryController(LaptopHarbourDbContext context)
        {
            _context = context;
        }

        // -------------------------------------------------------------
        // GET: api/VariantPriceHistory → All price history records
        // -------------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var data = await _context.VariantPriceHistories
                    .Select(vph => new VariantPriceHistoryDTO
                    {
                        OldPrice = vph.OldPrice,
                        NewPrice = vph.NewPrice,
                        CreatedAt = vph.CreatedAt
                    })
                    .ToListAsync();

                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to fetch price history records.", details = ex.Message });
            }
        }

        // -------------------------------------------------------------
        // GET: api/VariantPriceHistory/variant/{variantId} → Price history for a variant
        // -------------------------------------------------------------
        [HttpGet("variant/{variantId:int}")]
        public async Task<IActionResult> GetByVariant(int variantId)
        {
            try
            {
                var variantExists = await _context.ProductVariants.AnyAsync(v => v.VariantId == variantId);
                if (!variantExists)
                    return NotFound(new { error = "Variant not found." });

                var data = await _context.VariantPriceHistories
                    .Where(vph => vph.VariantId == variantId)
                    .OrderByDescending(vph => vph.CreatedAt)
                    .Select(vph => new VariantPriceHistoryDTO
                    {
                        OldPrice = vph.OldPrice,
                        NewPrice = vph.NewPrice,
                        CreatedAt = vph.CreatedAt
                    })
                    .ToListAsync();

                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to fetch price history for the variant.", details = ex.Message });
            }
        }

        // -------------------------------------------------------------
        // POST: api/VariantPriceHistory → Add new price history
        // -------------------------------------------------------------
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateVariantPriceHistory model)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var variantExists = await _context.ProductVariants.AnyAsync(v => v.VariantId == model.VariantId);
                if (!variantExists)
                    return NotFound(new { error = "Variant not found." });

                var priceHistory = new VariantPriceHistory
                {
                    VariantId = model.VariantId,
                    OldPrice = model.OldPrice,
                    NewPrice = model.NewPrice,
                    CreatedAt = DateTime.UtcNow
                };

                _context.VariantPriceHistories.Add(priceHistory);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { message = "Price history added successfully.", id = priceHistory.Id });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { error = "Failed to add price history.", details = ex.Message });
            }
        }

        // -------------------------------------------------------------
        // PUT: api/VariantPriceHistory/{id} → Update a price history record
        // -------------------------------------------------------------
        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] CreateVariantPriceHistory model)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var priceHistory = await _context.VariantPriceHistories.FindAsync(id);
                if (priceHistory == null)
                    return NotFound(new { error = "Price history record not found." });

                var variantExists = await _context.ProductVariants.AnyAsync(v => v.VariantId == model.VariantId);
                if (!variantExists)
                    return NotFound(new { error = "Variant not found." });

                priceHistory.VariantId = model.VariantId;
                priceHistory.OldPrice = model.OldPrice;
                priceHistory.NewPrice = model.NewPrice;
                priceHistory.CreatedAt = DateTime.UtcNow;

                _context.VariantPriceHistories.Update(priceHistory);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { message = "Price history updated successfully." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { error = "Failed to update price history.", details = ex.Message });
            }
        }

        // -------------------------------------------------------------
        // DELETE: api/VariantPriceHistory/{id} → Delete a price history record
        // -------------------------------------------------------------
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var priceHistory = await _context.VariantPriceHistories.FindAsync(id);
                if (priceHistory == null)
                    return NotFound(new { error = "Price history record not found." });

                _context.VariantPriceHistories.Remove(priceHistory);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { message = "Price history deleted successfully." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { error = "Failed to delete price history.", details = ex.Message });
            }
        }

        // -------------------------------------------------------------
        // DELETE: api/VariantPriceHistory/variant/{variantId} → Delete all price history of a variant
        // -------------------------------------------------------------
        [HttpDelete("variant/{variantId:int}")]
        public async Task<IActionResult> DeleteAllByVariant(int variantId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var histories = await _context.VariantPriceHistories
                    .Where(vph => vph.VariantId == variantId)
                    .ToListAsync();

                if (!histories.Any())
                    return NotFound(new { error = "No price history found for this variant." });

                _context.VariantPriceHistories.RemoveRange(histories);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { message = "All price history records for this variant deleted successfully." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { error = "Failed to delete price history records for the variant.", details = ex.Message });
            }
        }
    }
}
