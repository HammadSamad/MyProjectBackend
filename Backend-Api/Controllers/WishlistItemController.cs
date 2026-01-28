using Backend_Api.Data;
using Backend_Api.Models;
using Backend_Api.Models.Model_DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

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

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserWishlist(int userId)
        {
            try
            {
                var wishlist = await _context.Wishlists
                    .Include(w => w.WishlistItems)
                        .ThenInclude(wi => wi.Variant)
                            .ThenInclude(v => v.Product)
                                .ThenInclude(p => p.ProductImages)
                    .FirstOrDefaultAsync(w => w.UserId == userId);

                if (wishlist == null)
                    return NotFound(new { error = "Wishlist not found." });

                var wishlistDTO = new
                {
                    wishlist.WishlistId,
                    wishlist.UserId,
                    Items = wishlist.WishlistItems.Select(MapToDTO).ToList()
                };

                return Ok(wishlistDTO);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve wishlist.", details = ex.Message });
            }
        }

        //  wishlistId + variantId list
        // ============================
        [HttpGet("wishlist/{wishlistId}")]
        public async Task<IActionResult> GetWishlistVariants(int wishlistId)
        {
            if (wishlistId <= 0)
                return BadRequest(new { message = "Invalid wishlistId." });

            try
            {
                var exists = await _context.Wishlists
                    .AnyAsync(w => w.WishlistId == wishlistId);

                if (!exists)
                    return NotFound(new { message = "Wishlist not found." });

                var variants = await _context.WishlistItems
                    .Where(wi => wi.WishlistId == wishlistId)
                    .Select(wi => wi.VariantId)
                    .ToListAsync();

                return Ok(new
                {
                    wishlistId = wishlistId,
                    variantIds = variants
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Failed to retrieve wishlist variants.",
                    details = ex.Message
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddItem(AddWishlistItemDTO model)
        {
            if (model == null)
                return BadRequest(new { error = "Request body cannot be empty." });

            try
            {
                var wishlist = await _context.Wishlists
                    .Include(w => w.WishlistItems)
                        .ThenInclude(wi => wi.Variant)
                            .ThenInclude(v => v.Product)
                                .ThenInclude(p => p.ProductImages)
                    .FirstOrDefaultAsync(w => w.UserId == model.UserId);

                if (wishlist == null)
                {
                    wishlist = new Wishlist
                    {
                        UserId = model.UserId,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Wishlists.Add(wishlist);
                    await _context.SaveChangesAsync();
                }

                var variant = await _context.ProductVariants
                    .Include(v => v.Product)
                        .ThenInclude(p => p.ProductImages)
                    .FirstOrDefaultAsync(v => v.VariantId == model.VariantId);

                if (variant == null)
                    return NotFound(new { error = "Product variant not found." });

                bool exists = await _context.WishlistItems
                    .AnyAsync(x => x.WishlistId == wishlist.WishlistId && x.VariantId == variant.VariantId);

                if (exists)
                    return BadRequest(new { error = "This product already exists in the wishlist." });

                var item = new WishlistItem
                {
                    WishlistId = wishlist.WishlistId,
                    VariantId = variant.VariantId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.WishlistItems.Add(item);
                await _context.SaveChangesAsync();

                var allItems = await _context.WishlistItems
                    .Where(wi => wi.WishlistId == wishlist.WishlistId)
                    .Include(wi => wi.Variant)
                        .ThenInclude(v => v.Product)
                            .ThenInclude(p => p.ProductImages)
                    .ToListAsync();

                var wishlistDTO = new
                {
                    wishlist.WishlistId,
                    wishlist.UserId,
                    Items = allItems.Select(MapToDTO).ToList()
                };

                return Ok(wishlistDTO);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to add item to wishlist.", details = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteItem(int id)
        {
            try
            {
                var item = await _context.WishlistItems
                    .Include(wi => wi.Variant)
                    .ThenInclude(v => v.Product)
                    .ThenInclude(p => p.ProductImages)
                    .FirstOrDefaultAsync(x => x.WishlistItemId == id);

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

        private WishlistItemDTO MapToDTO(WishlistItem wi)
        {
            var variant = wi.Variant;
            var product = variant.Product;

            // ✅ Fix nullable bool issue here
            string coverImage = product.ProductImages.FirstOrDefault(i => i.IsCover == true)?.ImageUrl ?? "";

            decimal finalPrice = variant.Price ?? 0;
            var now = DateTime.UtcNow;

            if ((!variant.DiscountStart.HasValue || variant.DiscountStart.Value <= now) &&
                (!variant.DiscountEnd.HasValue || variant.DiscountEnd.Value >= now))
            {
                if (variant.DiscountPercentage.HasValue && variant.DiscountPercentage.Value > 0)
                    finalPrice -= finalPrice * (variant.DiscountPercentage.Value / 100);

                if (variant.DiscountAmount.HasValue && variant.DiscountAmount.Value > 0)
                    finalPrice -= variant.DiscountAmount.Value;
            }

            if (finalPrice < 0) finalPrice = 0;

            return new WishlistItemDTO
            {
                WishlistItemId = wi.WishlistItemId,
                WishlistId = wi.WishlistId,
                VariantId = wi.VariantId,
                ProductName = product.ProductName ?? "",
                Image = coverImage,
                Price = finalPrice
            };
        }
    }
}
