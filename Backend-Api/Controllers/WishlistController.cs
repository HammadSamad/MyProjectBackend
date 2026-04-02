using Backend_Api.Data;
using Backend_Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace Backend_Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WishlistController : ControllerBase
    {
        private readonly LaptopHarbourDbContext _context;

        public WishlistController(LaptopHarbourDbContext context)
        {
            _context = context;
        }

        // Create a wishlist for a user
        [HttpPost("Create/{userId}")]
        public async Task<IActionResult> CreateWishlist(int userId)
        {
            try
            {
                var existing = await _context.Wishlists.FirstOrDefaultAsync(w => w.UserId == userId);
                if (existing != null)
                    return BadRequest(new { error = "Wishlist already exists for this user." });

                var wishlist = new Wishlist
                {
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Wishlists.Add(wishlist);
                await _context.SaveChangesAsync();

                return Ok(new { wishlist.WishlistId, wishlist.UserId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to create wishlist.", details = ex.Message });
            }
        }

        // Get user's wishlist (basic info only, no items)
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetWishlist(int userId)
        {
            var wishlist = await _context.Wishlists.FirstOrDefaultAsync(w => w.UserId == userId);
            if (wishlist == null)
                return NotFound(new { error = "Wishlist not found." });

            return Ok(new { wishlist.WishlistId, wishlist.UserId, wishlist.CreatedAt });
        }

        // Delete a wishlist (including all items)
        [HttpDelete("{wishlistId}")]
        public async Task<IActionResult> DeleteWishlist(int wishlistId)
        {
            var wishlist = await _context.Wishlists
                .Include(w => w.WishlistItems)
                .FirstOrDefaultAsync(w => w.WishlistId == wishlistId);

            if (wishlist == null)
                return NotFound(new { error = "Wishlist not found." });

            _context.WishlistItems.RemoveRange(wishlist.WishlistItems);
            _context.Wishlists.Remove(wishlist);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Wishlist deleted successfully." });
        }
    }
}
