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

        // -------------------------------------------------------------
        // GET: api/VariantPriceHistory/variant/5 → Price history for a variant
        // -------------------------------------------------------------
        [HttpGet("variant/{variantId:int}")]
        public async Task<IActionResult> GetByVariant(int variantId)
        {
            var exists = await _context.ProductVariants.AnyAsync(v => v.VariantId == variantId);
            if (!exists) return NotFound("Variant not found.");

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

        // -------------------------------------------------------------
        // POST: api/VariantPriceHistory → Add new price history
        // -------------------------------------------------------------
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateVariantPriceHistory model)
        {
            // Validate variant
            var variantExists = await _context.ProductVariants.AnyAsync(v => v.VariantId == model.VariantId);
            if (!variantExists)
                return NotFound("Variant not found.");

            var priceHistory = new VariantPriceHistory
            {
                VariantId = model.VariantId,
                OldPrice = model.OldPrice,
                NewPrice = model.NewPrice,
                CreatedAt = DateTime.UtcNow
            };

            _context.VariantPriceHistories.Add(priceHistory);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Price history added successfully." });
        }

        // -------------------------------------------------------------
        // PUT: api/VariantPriceHistory/{id} → Update a price history record
        // -------------------------------------------------------------
        [HttpPut("{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] CreateVariantPriceHistory model)
        {
            var priceHistory = await _context.VariantPriceHistories.FindAsync(id);
            if (priceHistory == null)
                return NotFound("Price history record not found.");

            // Validate variant
            var variantExists = await _context.ProductVariants.AnyAsync(v => v.VariantId == model.VariantId);
            if (!variantExists)
                return NotFound("Variant not found.");

            priceHistory.VariantId = model.VariantId;
            priceHistory.OldPrice = model.OldPrice;
            priceHistory.NewPrice = model.NewPrice;
            priceHistory.CreatedAt = DateTime.UtcNow;

            _context.VariantPriceHistories.Update(priceHistory);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Price history updated successfully." });
        }

        // -------------------------------------------------------------
        // DELETE: api/VariantPriceHistory/{id} → Delete a price history record
        // -------------------------------------------------------------
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            var priceHistory = await _context.VariantPriceHistories.FindAsync(id);
            if (priceHistory == null)
                return NotFound("Price history record not found.");

            _context.VariantPriceHistories.Remove(priceHistory);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Price history deleted successfully." });
        }

        // -------------------------------------------------------------
        // DELETE: api/VariantPriceHistory/variant/5 → Delete all price history of a variant
        // -------------------------------------------------------------
        [HttpDelete("variant/{variantId:int}")]
        public async Task<IActionResult> DeleteAllByVariant(int variantId)
        {
            var histories = await _context.VariantPriceHistories
                .Where(vph => vph.VariantId == variantId)
                .ToListAsync();

            if (histories.Count == 0)
                return NotFound("No price history found for this variant.");

            _context.VariantPriceHistories.RemoveRange(histories);
            await _context.SaveChangesAsync();

            return Ok(new { message = "All price history records for this variant deleted successfully." });
        }
    }
}
