using Backend_Api.Data;
using Backend_Api.Models;
using Backend_Api.Models.Model_DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend_Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WishlistItemController : ControllerBase
    {
        private readonly LaptopHarbourDbContext _context;

        public WishlistItemController(LaptopHarbourDbContext context)
        {
            _context = context;
        }

        // ============================
        // GET: api/WishlistItem/wishlist/{wishlistId}
        // Get all items in a wishlist
        // ============================
        [HttpGet("wishlist/{wishlistId}")]
        public async Task<IActionResult> GetItemsByWishlist(int wishlistId)
        {
            try
            {
                var items = await _context.WishlistItems
                    .Where(x => x.WishlistId == wishlistId)
                    .Select(x => new WishlistItemDTO
                    {
                        WishlistItemId = x.WishlistItemId,
                        WishlistId = x.WishlistId,
                        VariantId = x.VariantId
                    })
                    .ToListAsync();

                if (items.Count == 0)
                    return NotFound(new { error = "No items found in this wishlist." });

                return Ok(items);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve wishlist items.", details = ex.Message });
            }
        }

        // ============================
        // POST: api/WishlistItem
        // Add item to wishlist
        // ============================
        [HttpPost]
        public async Task<IActionResult> AddItem(AddWishlistItemDTO model)
        {
            if (model == null)
                return BadRequest(new { error = "Request body cannot be empty." });

            try
            {
                // Check wishlist exists
                var wishlist = await _context.Wishlists.FindAsync(model.WishlistId);
                if (wishlist == null)
                    return NotFound(new { error = "Wishlist not found." });

                // Prevent duplicate product in wishlist
                bool exists = await _context.WishlistItems
                    .AnyAsync(x => x.WishlistId == model.WishlistId && x.VariantId == model.VariantId);

                if (exists)
                    return BadRequest(new { error = "This product already exists in the wishlist." });

                var item = new WishlistItem
                {
                    WishlistId = model.WishlistId,
                    VariantId = model.VariantId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.WishlistItems.Add(item);
                await _context.SaveChangesAsync();

                return Ok(new WishlistItemDTO
                {
                    WishlistItemId = item.WishlistItemId,
                    WishlistId = item.WishlistId,
                    VariantId = item.VariantId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to add item to wishlist.", details = ex.Message });
            }
        }

        // ============================
        // DELETE: api/WishlistItem/{id}
        // Remove item from wishlist
        // ============================
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteItem(int id)
        {
            try
            {
                var item = await _context.WishlistItems.FindAsync(id);
                if (item == null)
                    return NotFound(new { error = "Wishlist item not found." });

                _context.WishlistItems.Remove(item);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Item removed from wishlist." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to remove item from wishlist.", details = ex.Message });
            }
        }

        // ============================
        // DELETE: api/WishlistItem/wishlist/{wishlistId}/variant/{variantId}
        // Alternative remove using WishlistId + VariantId
        // ============================
        [HttpDelete("wishlist/{wishlistId}/variant/{variantId}")]
        public async Task<IActionResult> DeleteByVariant(int wishlistId, int variantId)
        {
            try
            {
                var item = await _context.WishlistItems
                    .FirstOrDefaultAsync(x => x.WishlistId == wishlistId && x.VariantId == variantId);

                if (item == null)
                    return NotFound(new { error = "Wishlist item not found." });

                _context.WishlistItems.Remove(item);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Item removed from wishlist." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to remove item from wishlist.", details = ex.Message });
            }
        }
    }
}
