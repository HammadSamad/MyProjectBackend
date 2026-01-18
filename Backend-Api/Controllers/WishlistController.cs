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
    public class WishlistController : ControllerBase
    {
        private readonly LaptopHarbourDbContext _context;

        public WishlistController(LaptopHarbourDbContext context)
        {
            _context = context;
        }

        // ============================
        // GET: api/Wishlist/user/{userId}
        // Get user's wishlist (auto create if not exists)
        // ============================
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserWishlist(int userId)
        {
            var wishlist = await _context.Wishlists
                .Include(w => w.WishlistItems)
                .ThenInclude(wi => wi.Variant)
                .FirstOrDefaultAsync(w => w.UserId == userId);

            if (wishlist == null)
            {
                wishlist = new Wishlist
                {
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Wishlists.Add(wishlist);
                await _context.SaveChangesAsync();
            }

            var result = new WishlistDTO
            {
                WishlistId = wishlist.WishlistId,
                UserId = wishlist.UserId ?? 0
            };

            return Ok(result);
        }

        // ============================
        // POST: api/Wishlist
        // Create wishlist manually (Admin use only)
        // ============================
        [HttpPost]
        public async Task<IActionResult> CreateWishlist(CreateWishlist model)
        {
            var exists = await _context.Wishlists.AnyAsync(w => w.UserId == model.UserId);
            if (exists)
                return BadRequest("Wishlist already exists for this user.");

            var wishlist = new Wishlist
            {
                UserId = model.UserId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Wishlists.Add(wishlist);
            await _context.SaveChangesAsync();

            return Ok(new WishlistDTO
            {
                WishlistId = wishlist.WishlistId,
                UserId = wishlist.UserId ?? 0
            });
        }

        // ============================
        // GET: api/Wishlist/{id}
        // ============================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetWishlistById(int id)
        {
            var wishlist = await _context.Wishlists.FindAsync(id);

            if (wishlist == null)
                return NotFound("Wishlist not found");

            return Ok(new WishlistDTO
            {
                WishlistId = wishlist.WishlistId,
                UserId = wishlist.UserId ?? 0
            });
        }

        // ============================
        // DELETE: api/Wishlist/{id}
        // Admin use (normally we do not delete wishlist)
        // ============================
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteWishlist(int id)
        {
            var wishlist = await _context.Wishlists
                .Include(w => w.WishlistItems)
                .FirstOrDefaultAsync(w => w.WishlistId == id);

            if (wishlist == null)
                return NotFound("Wishlist not found");

            _context.WishlistItems.RemoveRange(wishlist.WishlistItems);
            _context.Wishlists.Remove(wishlist);

            await _context.SaveChangesAsync();
            return Ok("Wishlist deleted successfully");
        }
    }
}
